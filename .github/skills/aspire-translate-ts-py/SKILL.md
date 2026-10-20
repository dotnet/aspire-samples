---
name: aspire-translate-ts-py
description: >
  Translate .NET/C# Aspire sample applications into TypeScript AppHost + Python API + React/TypeScript
  frontend equivalents. Use this skill when asked to create a TypeScript/Python version of an existing
  .NET Aspire sample, or to scaffold a new polyglot Aspire sample from scratch.
---

# Aspire Translate to TypeScript/Python Skill

This skill guides the translation of .NET/C# Aspire sample applications into polyglot equivalents
using a TypeScript AppHost, Python (FastAPI) API, and React/TypeScript frontend.

## Pre-flight

1. **Study the .NET sample**: Read every file in the source sample under `samples/<SampleName>/`.
   Identify:
   - AppHost resource graph (what resources are created and how they're wired)
   - API endpoints, models, and data access patterns
   - Frontend UI features and API client calls
   - Any shared projects or service defaults
2. **Choose a target folder name**: Convention is `<SampleName>TypeScript` under `samples/`.
3. **Check prerequisites**: Ensure `aspire` CLI is installed (`aspire --version`).

## Phase 1: Scaffold the Project

### 1.1 Create the target directory

```bash
mkdir samples/<SampleName>TypeScript
```

### 1.2 Scaffold with `aspire new`

Use the `aspire-py-starter` template for a TypeScript AppHost + Python API + React frontend:

```bash
cd samples/<SampleName>TypeScript
aspire new aspire-py-starter \
  --name <ProjectName> \
  --localhost-tld false \
  --use-redis-cache false \
  --non-interactive
```

- `--localhost-tld false` — Disable `*.dev.localhost` URLs (matches repo convention).
- `--use-redis-cache false` — Omit Redis unless the original sample uses it.

### 1.3 Add hosting integrations

For each Azure/infrastructure resource in the .NET AppHost, add the corresponding integration:

```bash
cd <ProjectName>
aspire add Aspire.Hosting.Azure.CosmosDB --non-interactive
aspire add Aspire.Hosting.Azure.Storage --non-interactive
# ... etc.
```

Use the **full NuGet package ID** (e.g. `Aspire.Hosting.Azure.CosmosDB`, not `Azure.CosmosDB`)
because the short names may not resolve in `--non-interactive` mode.

After adding integrations, verify the `.aspire/modules/aspire.mts` file has been regenerated
with the new resource types (e.g. `addAzureCosmosDB`).

## Phase 2: Translate the AppHost

### 2.1 Map .NET AppHost to TypeScript

Translate each line in the .NET `AppHost.cs` (or `Program.cs`) to the TypeScript equivalent
in `apphost.mts`.

**Common API mappings:**

| .NET C# | TypeScript |
|---------|-----------|
| `builder.AddAzureCosmosDB("name")` | `builder.addAzureCosmosDB("name")` |
| `.RunAsEmulator()` | `.runAsEmulator()` |
| `.AddCosmosDatabase("name")` | `.addCosmosDatabase("name")` |
| `builder.AddProject<T>("name")` | `builder.addUvicornApp("name", "./app", "main:app")` |
| `.WithReference(resource)` | `.withReference(resource)` |
| `.WaitFor(resource)` | `.waitFor(resource)` |
| `.WithExternalHttpEndpoints()` | `.withExternalHttpEndpoints()` |
| `builder.AddProject<Web>("frontend")` | `builder.addViteApp("frontend", "./frontend")` |

### 2.2 Emulator configuration for non-.NET SDKs

When the Python or TypeScript SDK connects to an Azure emulator running in Docker,
there are common connectivity issues that **do not affect .NET clients**:

#### Cosmos DB Emulator

The Cosmos DB emulator running in Docker reports its **container-internal IP** in gateway
metadata. The .NET SDK handles this transparently, but the Python SDK follows the redirect
to the unreachable internal IP. Fix with two changes:

**In `apphost.mts`** — Configure the emulator container:

```typescript
const cosmos = builder.addAzureCosmosDB("cosmos").runAsEmulator({
    configureContainer: async (emulator) => {
        // Fixed port so the host port matches the container port
        await emulator.withGatewayPort({ port: 8081 });
        // Make the emulator report 127.0.0.1 instead of its Docker IP
        await emulator.withEnvironment(
            "AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE", "127.0.0.1"
        );
    }
});
```

**In the Python API** — Handle connection string and SSL:

```python
# Parse the connection string manually because the Aspire-injected string
# includes .NET-specific settings (e.g. DisableServerCertificateValidation)
# that the Python SDK doesn't understand.
endpoint, key = _parse_connection_string(connection_string)

client = CosmosClient(
    url=endpoint,
    credential=key,
    connection_verify=False,           # Accept self-signed emulator cert
    enable_endpoint_discovery=False,   # Don't follow gateway redirects
)
```

Also add retry logic (the emulator can take time to fully start):

```python
MAX_RETRIES = 10
RETRY_DELAY_SECONDS = 3

for attempt in range(1, MAX_RETRIES + 1):
    try:
        client = CosmosClient(...)
        database = client.create_database_if_not_exists(db_name)
        container = database.create_container_if_not_exists(...)
        break
    except Exception as ex:
        logger.warning(f"Attempt {attempt}/{MAX_RETRIES} failed: {ex}")
        if attempt < MAX_RETRIES:
            time.sleep(RETRY_DELAY_SECONDS)
        else:
            raise
```

### 2.3 Resource wiring pattern

The standard wiring pattern mirrors the .NET version:

```
infrastructure resource → database → API service → frontend
```

In the AppHost:

```typescript
const cosmos = builder.addAzureCosmosDB("cosmos").runAsEmulator({...});
const tododb = cosmos.addCosmosDatabase("tododb");

const app = await builder
    .addUvicornApp("app", "./app", "main:app")
    .withUv()
    .withExternalHttpEndpoints()
    .withHttpHealthCheck({ path: "/health" })
    .withReference(cosmos)    // Pass connection info to the API
    .waitFor(tododb);         // Wait for DB to be ready

const frontend = await builder
    .addViteApp("frontend", "./frontend")
    .withReference(app)       // Inject API URL into frontend
    .waitFor(app);            // Wait for API to be healthy

await app.publishWithContainerFiles(frontend, "./static");
await builder.build().run();
```

## Phase 3: Translate the Python API

### 3.1 Update `pyproject.toml`

Add the required Azure SDK packages:

```toml
[project]
name = "my-api"
version = "0.1.0"
requires-python = ">=3.13"
dependencies = [
    "fastapi[standard]>=0.119.0",
    "azure-cosmos>=4.9.0",
    "pydantic>=2.0.0",
    "opentelemetry-distro>=0.59b0",
    "opentelemetry-exporter-otlp-proto-grpc>=1.38.0",
    "opentelemetry-instrumentation-fastapi>=0.59b0",
]
```

### 3.2 Map .NET models to Pydantic

Translate C# `record` types to Pydantic `BaseModel` classes:

```csharp
// C#
public record Todo(string Description, string id, string UserId, bool IsComplete = false);
```

```python
# Python
class Todo(BaseModel):
    id: str
    description: str
    userId: str
    isComplete: bool = False
```

Also create a separate "create" model for POST requests:

```python
class TodoCreate(BaseModel):
    description: str
    userId: str = "sampleuser"
```

### 3.3 Map .NET endpoints to FastAPI routes

| .NET | FastAPI |
|------|---------|
| `app.MapPost("/todos", ...)` | `@app.post("/api/todos")` |
| `app.MapGet("/todos", ...)` | `@app.get("/api/todos")` |
| `app.MapPut("/todos/{id}", ...)` | `@app.put("/api/todos/{todo_id}")` |
| `app.MapDelete("/todos/{userId}/{id}", ...)` | `@app.delete("/api/todos/{user_id}/{todo_id}")` |

**Important**: Prefix all API routes with `/api/` so the Vite proxy can forward them correctly.

### 3.4 Environment variables

Aspire injects connection properties as environment variables named `{RESOURCE}_{PROPERTY}`:

```python
connection_string = os.getenv("COSMOS_CONNECTIONSTRING")
db_name = os.getenv("COSMOS_DATABASENAME", "tododb")
```

The resource name in the AppHost determines the prefix (e.g. `cosmos` → `COSMOS_`).

### 3.5 Cosmos DB data access

Map .NET Cosmos SDK calls to Python equivalents:

| .NET SDK | Python SDK |
|----------|-----------|
| `container.CreateItemAsync(item)` | `container.create_item(body=item)` |
| `container.ReadItemAsync(id, pk)` | `container.read_item(item=id, partition_key=pk)` |
| `container.ReplaceItemAsync(item, id)` | `container.replace_item(item=id, body=item)` |
| `container.DeleteItemAsync(id, pk)` | `container.delete_item(item=id, partition_key=pk)` |
| LINQ query (`.GetItemLinqQueryable()`) | `container.read_all_items()` |
| `database.CreateDatabaseIfNotExistsAsync()` | `client.create_database_if_not_exists()` |
| `database.CreateContainerIfNotExistsAsync()` | `database.create_container_if_not_exists()` |

### 3.6 Health check

Keep the `/health` endpoint from the template:

```python
@app.get("/health", response_class=fastapi.responses.PlainTextResponse)
async def health_check():
    return "Healthy"
```

## Phase 4: Translate the Frontend

### 4.1 Vite proxy configuration

The template generates a `vite.config.ts` that proxies `/api` calls to the Python backend.
This works automatically when routes are prefixed with `/api/`:

```typescript
server: {
    proxy: {
        '/api': {
            target: process.env.APP_HTTPS || process.env.APP_HTTP,
            changeOrigin: true
        }
    }
}
```

### 4.2 Map Blazor/Razor components to React

- **Data tables** → React state with `useState` + `useEffect` for loading
- **Button actions** → `onClick` handlers calling `fetch()` with the appropriate HTTP method
- **Form inputs** → Controlled inputs with `useState`
- **Loading states** → Conditional rendering with loading skeletons

### 4.3 API client pattern

Use `fetch()` directly (no separate client library needed):

```typescript
// GET
const response = await fetch('/api/todos');
const data: Todo[] = await response.json();

// POST
await fetch('/api/todos', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ description, userId: 'sampleuser' }),
});

// PUT
await fetch(`/api/todos/${todo.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ...todo, isComplete: !todo.isComplete }),
});

// DELETE
await fetch(`/api/todos/${todo.userId}/${todo.id}`, { method: 'DELETE' });
```

### 4.4 Update page title and branding

- Update `index.html` `<title>` to match the sample purpose.
- Update the header in `App.tsx` (title, subtitle).

## Phase 5: Verification

### 5.1 Build checks

```bash
# AppHost TypeScript
cd <ProjectName>
npm run build          # Lints + compiles apphost.mts

# Frontend
cd frontend
npm install            # Required after scaffold
npm run build          # TypeScript + Vite build
npm run lint           # ESLint
```

### 5.2 Runtime verification

```bash
cd <ProjectName>
aspire start
```

Then verify:

1. **All resources healthy**: Check the Aspire dashboard — all resources should reach
   "Running" + "Healthy" status.
2. **API works**: `curl -k https://localhost:<port>/api/todos` returns `[]`.
3. **CRUD works**: Test POST, GET, PUT, DELETE via curl or PowerShell.
4. **Frontend works**: Open the frontend URL in a browser — the UI should load and
   interact with the API.

### 5.3 Common issues

| Symptom | Cause | Fix |
|---------|-------|-----|
| `ServiceRequestTimeoutError` connecting to Docker internal IP | Cosmos emulator reports container IP | Add `AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE=127.0.0.1` and `withGatewayPort` |
| SSL certificate errors | Emulator uses self-signed cert | Use `connection_verify=False` in Python SDK |
| `from_connection_string` ignores settings | Aspire injects .NET-specific connection string keys | Parse manually and construct `CosmosClient(url=..., credential=...)` |
| Frontend can't reach API | Vite proxy misconfigured | Ensure API routes start with `/api/` and proxy target uses `APP_HTTPS`/`APP_HTTP` env vars |
| `npm run build` fails in frontend | Dependencies not installed | Run `npm install` in the `frontend/` directory first |

## Reference: Complete File Inventory

A translated sample should contain:

```
samples/<SampleName>TypeScript/<ProjectName>/
├── apphost.mts              # TypeScript AppHost (resource graph)
├── aspire.config.json       # Aspire configuration (auto-generated)
├── package.json             # AppHost npm dependencies
├── tsconfig.apphost.json    # TypeScript config for AppHost
├── eslint.config.mjs        # ESLint config for AppHost
├── .aspire/                 # Auto-generated Aspire modules (do not edit)
├── app/                     # Python FastAPI API
│   ├── main.py              # API endpoints and Cosmos DB access
│   ├── telemetry.py         # OpenTelemetry configuration (from template)
│   ├── pyproject.toml       # Python dependencies
│   └── .python-version      # Python version (3.13)
└── frontend/                # React/TypeScript frontend
    ├── src/
    │   ├── App.tsx           # Main UI component
    │   ├── App.css           # Styles
    │   ├── main.tsx          # React entry point
    │   └── index.css         # Global styles (from template)
    ├── index.html            # HTML entry point
    ├── package.json          # Frontend npm dependencies
    ├── vite.config.ts        # Vite config with API proxy
    └── tsconfig.*.json       # TypeScript configs
```
