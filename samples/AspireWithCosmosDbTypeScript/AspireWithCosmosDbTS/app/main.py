import contextlib
import logging
import os
import time
import uuid

import fastapi
import fastapi.responses
import fastapi.staticfiles
import opentelemetry.instrumentation.fastapi as otel_fastapi
import telemetry
from azure.cosmos import CosmosClient, PartitionKey, exceptions
from pydantic import BaseModel


DATABASE_NAME = "tododb"
CONTAINER_NAME = "todos"
PARTITION_KEY_PATH = "/UserId"
DEFAULT_USER_ID = "sampleuser"

MAX_RETRIES = 10
RETRY_DELAY_SECONDS = 3


class TodoCreate(BaseModel):
    description: str
    userId: str = DEFAULT_USER_ID


class Todo(BaseModel):
    id: str
    description: str
    userId: str
    isComplete: bool = False


cosmos_container = None


def _parse_connection_string(conn_str: str) -> tuple[str, str]:
    """Parse AccountEndpoint and AccountKey from a Cosmos DB connection string."""
    parts = dict(
        part.split("=", 1)
        for part in conn_str.split(";")
        if "=" in part
    )
    endpoint = parts.get("AccountEndpoint", "")
    key = parts.get("AccountKey", "")
    return endpoint, key


def get_cosmos_container():
    """Get or create the Cosmos DB container, retrying until the emulator is ready."""
    global cosmos_container
    if cosmos_container is not None:
        return cosmos_container

    connection_string = os.getenv("COSMOS_CONNECTIONSTRING", "")
    db_name = os.getenv("COSMOS_DATABASENAME", DATABASE_NAME)

    # Parse the connection string to extract the endpoint and key.
    # The Aspire-injected connection string includes .NET-specific settings
    # like DisableServerCertificateValidation that the Python SDK doesn't
    # understand, so we construct the client explicitly.
    endpoint, key = _parse_connection_string(connection_string)
    logger.info(f"Connecting to Cosmos DB at {endpoint}, database: {db_name}")

    # The Cosmos DB emulator uses a self-signed certificate, so we must
    # disable SSL verification. We also retry because the emulator can
    # take time to become ready.
    for attempt in range(1, MAX_RETRIES + 1):
        try:
            client = CosmosClient(
                url=endpoint,
                credential=key,
                connection_verify=False,
                enable_endpoint_discovery=False,
            )
            database = client.create_database_if_not_exists(db_name)
            container = database.create_container_if_not_exists(
                id=CONTAINER_NAME,
                partition_key=PartitionKey(path=PARTITION_KEY_PATH),
            )
            cosmos_container = container
            logger.info("Connected to Cosmos DB successfully.")
            return container
        except Exception as ex:
            logger.warning(
                f"Cosmos DB connection attempt {attempt}/{MAX_RETRIES} failed: {ex}"
            )
            if attempt < MAX_RETRIES:
                time.sleep(RETRY_DELAY_SECONDS)
            else:
                raise


@contextlib.asynccontextmanager
async def lifespan(app):
    telemetry.configure_opentelemetry()
    yield


app = fastapi.FastAPI(lifespan=lifespan)
otel_fastapi.FastAPIInstrumentor.instrument_app(app, exclude_spans=["send"])

logger = logging.getLogger(__name__)


if not os.path.exists("static"):
    @app.get("/", response_class=fastapi.responses.HTMLResponse)
    async def root():
        """Root endpoint."""
        return "API service is running. Navigate to <a href='/api/todos'>/api/todos</a> to see TODO items."


@app.post("/api/todos", response_model=Todo)
async def create_todo(todo: TodoCreate):
    """Create a new TODO item."""
    container = get_cosmos_container()
    item = {
        "id": str(uuid.uuid4()),
        "description": todo.description,
        "UserId": todo.userId,
        "isComplete": False,
    }
    created = container.create_item(body=item)
    return Todo(
        id=created["id"],
        description=created["description"],
        userId=created["UserId"],
        isComplete=created["isComplete"],
    )


@app.get("/api/todos", response_model=list[Todo])
async def get_todos():
    """Get all TODO items."""
    container = get_cosmos_container()
    items = list(container.read_all_items())
    return [
        Todo(
            id=item["id"],
            description=item["description"],
            userId=item["UserId"],
            isComplete=item["isComplete"],
        )
        for item in items
    ]


@app.put("/api/todos/{todo_id}", response_model=Todo)
async def update_todo(todo_id: str, todo: Todo):
    """Update an existing TODO item."""
    container = get_cosmos_container()
    item = {
        "id": todo.id,
        "description": todo.description,
        "UserId": todo.userId,
        "isComplete": todo.isComplete,
    }
    updated = container.replace_item(item=todo_id, body=item)
    return Todo(
        id=updated["id"],
        description=updated["description"],
        userId=updated["UserId"],
        isComplete=updated["isComplete"],
    )


@app.delete("/api/todos/{user_id}/{todo_id}")
async def delete_todo(user_id: str, todo_id: str):
    """Delete a TODO item."""
    container = get_cosmos_container()
    try:
        container.delete_item(item=todo_id, partition_key=user_id)
        return {"status": "deleted"}
    except exceptions.CosmosResourceNotFoundError:
        raise fastapi.HTTPException(status_code=404, detail="Todo not found")


@app.get("/health", response_class=fastapi.responses.PlainTextResponse)
async def health_check():
    """Health check endpoint."""
    return "Healthy"


# Serve static files directly from root, if the "static" directory exists
if os.path.exists("static"):
    app.mount(
        "/",
        fastapi.staticfiles.StaticFiles(directory="static", html=True),
        name="static"
    )
