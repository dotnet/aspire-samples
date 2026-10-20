import contextlib
import datetime
import logging
import os
import random

import fastapi
import fastapi.responses
import fastapi.staticfiles
import openai
import opentelemetry.instrumentation.fastapi as otel_fastapi
import pydantic
import telemetry


@contextlib.asynccontextmanager
async def lifespan(app):
    telemetry.configure_opentelemetry()
    yield


app = fastapi.FastAPI(lifespan=lifespan)
otel_fastapi.FastAPIInstrumentor.instrument_app(app, exclude_spans=["send"])


logger = logging.getLogger(__name__)


# OpenAI client configured from Aspire-injected environment variables.
openai_client = openai.OpenAI(
    api_key=os.environ.get("CHAT_KEY", ""),
    base_url=os.environ.get("CHAT_ENDPOINT", "https://api.openai.com/v1"),
)
chat_model = os.environ.get("CHAT_MODELNAME", "gpt-4o-mini")


class ChatMessage(pydantic.BaseModel):
    role: str
    content: str


class ChatRequest(pydantic.BaseModel):
    messages: list[ChatMessage]


class ChatResponse(pydantic.BaseModel):
    message: ChatMessage


@app.post("/api/chat")
async def chat_completion(request: ChatRequest) -> ChatResponse:
    """Chat completion endpoint using OpenAI."""
    allowed_roles = {"user", "assistant"}
    for message in request.messages:
        if message.role not in allowed_roles:
            raise fastapi.HTTPException(
                status_code=400,
                detail=f"Invalid role '{message.role}'. Allowed roles are: {allowed_roles}",
            )
        
    messages = [{"role": m.role, "content": m.content} for m in request.messages]

    try:
        import asyncio

        response = await asyncio.to_thread(
            openai_client.chat.completions.create,
            model=chat_model,
            messages=messages,
        )
    except openai.RateLimitError as e:
        raise fastapi.HTTPException(
            status_code=429,
            detail="OpenAI quota exceeded. Please check your API plan and billing at https://platform.openai.com/settings/organization/billing",
        ) from e
    except openai.AuthenticationError as e:
        raise fastapi.HTTPException(
            status_code=401,
            detail="Invalid OpenAI API key. Please verify your key with: aspire secret set Parameters:openai-openai-apikey <your-key>",
        ) from e
    except openai.APIError as e:
        logger.error("OpenAI API error: %s", e)
        raise fastapi.HTTPException(
            status_code=502,
            detail=f"OpenAI API error: {e.message}",
        ) from e

    assistant_message = response.choices[0].message
    return ChatResponse(
        message=ChatMessage(role="assistant", content=assistant_message.content or "")
    )


if not os.path.exists("static"):
    @app.get("/", response_class=fastapi.responses.HTMLResponse)
    async def root():
        """Root endpoint."""
        return "API service is running. Navigate to <a href='/api/weatherforecast'>/api/weatherforecast</a> to see sample data."

@app.get("/api/weatherforecast")
async def weather_forecast():
    """Weather forecast endpoint."""
    # Generate fresh data if not in cache or cache unavailable.
    summaries = [
        "Freezing",
        "Bracing",
        "Chilly",
        "Cool",
        "Mild",
        "Warm",
        "Balmy",
        "Hot",
        "Sweltering",
        "Scorching",
    ]

    forecast = []
    for index in range(1, 6):  # Range 1 to 5 (inclusive)
        temp_c = random.randint(-20, 55)
        forecast_date = datetime.datetime.now() + datetime.timedelta(days=index)
        forecast_item = {
            "date": forecast_date.isoformat(),
            "temperatureC": temp_c,
            "temperatureF": int(temp_c * 9 / 5) + 32,
            "summary": random.choice(summaries),
        }
        forecast.append(forecast_item)

    return forecast


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
