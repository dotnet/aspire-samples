# Polyglot AppHost TypeScript Conversion Notes

This document logs all issues, gaps, limitations, and errors discovered while attempting to rewrite
each sample's `AppHost.cs` as a polyglot `apphost.ts` using the Aspire TypeScript SDK.

## Overview

Each sample directory now contains an `apphost.ts` file alongside the existing `AppHost.cs`. The
TypeScript versions use the Aspire polyglot apphost SDK (`createBuilder()` from `.modules/aspire.js`)
which communicates with a .NET AppHost Server via JSON-RPC.

### Prerequisites

To run the polyglot TypeScript apphosts:

1. Install the staging Aspire CLI:
   ```powershell
   iex "& { $(irm https://aspire.dev/install.ps1) } -Quality staging"
   ```
2. Node.js (v18+) or Bun must be installed
3. Run `aspire run` from the sample directory containing `apphost.ts`

The CLI will:
- Scaffold an AppHost server project
- Generate the TypeScript SDK in `.modules/`
- Start the .NET AppHost server + Node.js/Bun guest runtime
- The generated SDK provides typed builder classes for all available capabilities

### Skipped Sample

- **standalone-dashboard**: This is a standalone console application, not an Aspire AppHost sample. It
  configures OpenTelemetry directly and does not use `DistributedApplication.CreateBuilder()`.

---

## Per-Sample Gap Analysis

### 1. Metrics (`samples/Metrics/MetricsApp.AppHost/apphost.ts`)

**Convertible:** Partially  
**Status:** Container resources work, but custom extension and URL customization do not.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Add container with image/tag | `AddContainer("prometheus", "prom/prometheus", "v3.2.1")` | ✅ Available |
| Bind mount (read-only) | `.WithBindMount("../prometheus", "/etc/prometheus", isReadOnly: true)` | ✅ Available |
| Container args | `.WithArgs("--web.enable-otlp-receiver", ...)` | ✅ Available |
| HTTP endpoint | `.WithHttpEndpoint(targetPort: 9090)` | ✅ Available |
| URL display customization | `.WithUrlForEndpoint("http", u => u.DisplayText = "...")` | ❌ Lambda callback not available |
| Custom extension method | `AddOpenTelemetryCollector(...)` | ❌ Custom C# extension — not exported to ATS |
| Project reference (generic) | `AddProject<Projects.MetricsApp>("app")` | ⚠️ `addProject("app")` (no type-safe project binding) |
| Environment from endpoint | `.WithEnvironment("GRAFANA_URL", grafana.GetEndpoint("http"))` | ✅ Available |

**Key Limitation:** The `AddOpenTelemetryCollector` is a custom extension method defined in the sample's
AppHost project. Custom C# extensions require `[AspireExport]` attributes to be available in the
polyglot SDK, which this sample does not have.

---

### 2. aspire-shop (`samples/aspire-shop/AspireShop.AppHost/apphost.ts`)

**Convertible:** Mostly  
**Status:** Core resource orchestration works. HTTP commands and URL customization are gaps.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Postgres + PgAdmin | `AddPostgres("postgres").WithPgAdmin()` | ✅ Available |
| Container lifetime | `.WithLifetime(ContainerLifetime.Persistent)` | ✅ Available |
| Conditional data volume | `if (IsRunMode) postgres.WithDataVolume()` | ✅ Available (via `executionContext`) |
| Add database | `postgres.AddDatabase("catalogdb")` | ✅ Available |
| Redis + Commander | `AddRedis("basketcache").WithDataVolume().WithRedisCommander()` | ✅ Available |
| Project references | `AddProject<Projects.X>("name")` | ⚠️ `addProject("name")` — no generic type |
| HTTP health check | `.WithHttpHealthCheck("/health")` | ✅ Available |
| HTTP command | `.WithHttpCommand("/reset-db", "Reset Database", ...)` | ❌ Not available |
| URL display customization | `.WithUrlForEndpoint("https", url => url.DisplayText = "...")` | ❌ Lambda callback not available |
| External HTTP endpoints | `.WithExternalHttpEndpoints()` | ✅ Available |
| Resource references | `.WithReference(resource).WaitFor(resource)` | ✅ Available |

**Key Limitation:** `WithHttpCommand` is an advanced feature for adding custom dashboard commands.
This is not exposed through ATS capabilities.

---

### 3. aspire-with-javascript (`samples/aspire-with-javascript/AspireJavaScript.AppHost/apphost.ts`)

**Convertible:** Minimally  
**Status:** Almost entirely blocked — JavaScript/Node.js hosting APIs are not available.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Project reference | `AddProject<Projects.X>("weatherapi")` | ⚠️ `addProject("weatherapi")` |
| JavaScript app | `AddJavaScriptApp("angular", "../path", runScriptName: "start")` | ❌ Not available |
| Vite app | `AddViteApp("reactvite", "../path")` | ❌ Not available |
| Run script | `.WithRunScript("start")` | ❌ Not available |
| npm configuration | `.WithNpm(installCommand: "ci")` | ❌ Not available |
| Publish as Dockerfile | `.PublishAsDockerFile()` | ❌ Not available |
| Publish container files | `weatherApi.PublishWithContainerFiles(reactVite, "./wwwroot")` | ❌ Not available |
| HTTP endpoint with env | `.WithHttpEndpoint(env: "PORT")` | ❌ env parameter not available |

**Key Limitation:** The entire `Aspire.Hosting.JavaScript` package (AddJavaScriptApp, AddViteApp,
WithNpm, WithRunScript, PublishAsDockerFile) is not available in the polyglot SDK. This sample
is fundamentally about JavaScript hosting, making it almost entirely unconvertible.

---

### 4. aspire-with-node (`samples/aspire-with-node/AspireWithNode.AppHost/apphost.ts`)

**Convertible:** Partially  
**Status:** Redis and project resources work, but Node.js app hosting is not available.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Redis + Insight | `AddRedis("cache").WithRedisInsight()` | ✅ Available |
| Project reference | `AddProject<Projects.X>("weatherapi")` | ⚠️ `addProject("weatherapi")` |
| Node.js app | `AddNodeApp("frontend", "../NodeFrontend", "./app.js")` | ❌ Not available |
| npm config | `.WithNpm()` | ❌ Not available |
| Run script | `.WithRunScript("dev")` | ❌ Not available |
| HTTP endpoint with env | `.WithHttpEndpoint(port: 5223, env: "PORT")` | ❌ env parameter not available |

**Key Limitation:** `AddNodeApp` from `Aspire.Hosting.NodeJs` is not available. The Node.js
frontend cannot be orchestrated.

---

### 5. aspire-with-python (`samples/aspire-with-python/apphost.ts`)

**Convertible:** Minimally  
**Status:** Almost entirely blocked — Python and Vite hosting APIs are not available.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Redis | `AddRedis("cache")` | ✅ Available |
| Uvicorn app | `AddUvicornApp("app", "./app", "main:app")` | ❌ Not available |
| UV package manager | `.WithUv()` | ❌ Not available |
| Vite app | `AddViteApp("frontend", "./frontend")` | ❌ Not available |
| Publish container files | `app.PublishWithContainerFiles(frontend, "./static")` | ❌ Not available |

**Key Limitation:** Both `Aspire.Hosting.Python` (AddUvicornApp, WithUv) and
`Aspire.Hosting.JavaScript` (AddViteApp) packages are not available in the polyglot SDK.

---

### 6. client-apps-integration (`samples/client-apps-integration/ClientAppsIntegration.AppHost/apphost.ts`)

**Convertible:** Partially  
**Status:** API service project works, but Windows platform checks and desktop app features don't.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Project reference | `AddProject<Projects.X>("apiservice")` | ⚠️ `addProject("apiservice")` |
| Platform check | `OperatingSystem.IsWindows()` | ❌ Not available in TS context |
| Explicit start | `.WithExplicitStart()` | ❌ Not available |
| Exclude from manifest | `.ExcludeFromManifest()` | ❌ Not available |

**Key Limitation:** `OperatingSystem.IsWindows()` has no equivalent in the TypeScript SDK.
Desktop apps (WinForms, WPF) are Windows-only and their conditional inclusion cannot be expressed.

---

### 7. container-build (`samples/container-build/apphost.ts`)

**Convertible:** Minimally  
**Status:** Almost entirely blocked — Dockerfile build APIs are not available.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Parameter with default | `AddParameter("goversion", "1.25.4", publishValueAsDefault: true)` | ❌ Not available |
| Dockerfile build | `AddDockerfile("ginapp", "./ginapp")` | ❌ Not available |
| Build arg | `.WithBuildArg("GO_VERSION", goVersion)` | ❌ Not available |
| OTLP exporter | `.WithOtlpExporter()` | ❌ Not available |
| Certificate trust | `.WithDeveloperCertificateTrust(trust: true)` | ❌ Not available |
| Execution context | `builder.ExecutionContext.IsPublishMode` | ✅ Available (via `executionContext`) |

**Key Limitation:** `AddDockerfile` and `WithBuildArg` from `Aspire.Hosting` are not available.
This sample is fundamentally about building containers from Dockerfiles.

---

### 8. custom-resources (`samples/custom-resources/CustomResources.AppHost/apphost.ts`)

**Convertible:** Not convertible  
**Status:** Entirely blocked — custom resource types require C# implementation.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Custom resource | `AddTalkingClock("talking-clock")` | ❌ Custom C# extension |
| Custom resource | `AddTestResource("test")` | ❌ Custom C# extension |

**Key Limitation:** This sample demonstrates creating custom resource types in C#. These are
implemented as C# classes and extension methods. The polyglot SDK can only access capabilities
from NuGet packages with `[AspireExport]` attributes — custom project-level extensions are not
available.

---

### 9. database-containers (`samples/database-containers/DatabaseContainers.AppHost/apphost.ts`)

**Convertible:** Mostly  
**Status:** Core database hosting works with minor gaps.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Postgres | `AddPostgres("postgres")` | ✅ Available |
| MySQL | `AddMySql("mysql")` | ✅ Available |
| SQL Server | `AddSqlServer("sqlserver")` | ✅ Available |
| Environment variable | `.WithEnvironment("POSTGRES_DB", todosDbName)` | ✅ Available |
| Bind mount | `.WithBindMount(source, target)` | ✅ Available |
| Data volume | `.WithDataVolume()` | ✅ Available |
| Container lifetime | `.WithLifetime(ContainerLifetime.Persistent)` | ✅ Available |
| Add database | `.AddDatabase("name")` | ✅ Available |
| PgWeb | `.WithPgWeb()` | ❌ Not available |
| Creation script | `.WithCreationScript(File.ReadAllText(path))` | ❌ Not available (file I/O + API) |
| HTTP health check | `.WithHttpHealthCheck("/health")` | ✅ Available |

**Key Limitation:** `WithCreationScript` requires reading a SQL file from disk and passing its
content. The polyglot SDK doesn't have a `File.ReadAllText` equivalent, and `WithCreationScript`
itself may not be exported. `WithPgWeb` is also not available.

---

### 10. database-migrations (`samples/database-migrations/DatabaseMigrations.AppHost/apphost.ts`)

**Convertible:** Mostly  
**Status:** Works with minor API gap for WaitForCompletion.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| SQL Server + volume | `AddSqlServer("sqlserver").WithDataVolume()` | ✅ Available |
| Container lifetime | `.WithLifetime(ContainerLifetime.Persistent)` | ✅ Available |
| Add database | `.AddDatabase("db1")` | ✅ Available |
| Project references | `AddProject<Projects.X>("name")` | ⚠️ `addProject("name")` |
| Wait for completion | `.WaitForCompletion(migrationService)` | ❌ Only `waitFor` available |
| Resource references | `.WithReference(db1)` | ✅ Available |

**Key Limitation:** `WaitForCompletion` (wait for a service to finish, not just start) is not
available. This is semantically different from `WaitFor` — it waits for the migration service to
complete its work before starting the API.

---

### 11. health-checks-ui (`samples/health-checks-ui/HealthChecksUI.AppHost/apphost.ts`)

**Convertible:** Partially  
**Status:** Core project orchestration works, but specialized features don't.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Docker Compose | `AddDockerComposeEnvironment("compose")` | ❌ Not available |
| Redis | `AddRedis("cache")` | ✅ Available |
| Project references | `AddProject<Projects.X>("name")` | ⚠️ `addProject("name")` |
| Health checks UI | `AddHealthChecksUI("healthchecksui")` | ❌ Not available |
| HTTP probe | `.WithHttpProbe(ProbeType.Liveness, "/alive")` | ❌ Not available |
| Custom URL helper | `.WithFriendlyUrls(...)` | ❌ Custom extension method |
| Host port | `.WithHostPort(7230)` | ❌ Not available |
| Execution context | `builder.ExecutionContext.IsRunMode` | ✅ Available |

**Key Limitation:** `AddDockerComposeEnvironment`, `AddHealthChecksUI`, and `WithHttpProbe` are
specialized integrations not available in the polyglot SDK. The `WithFriendlyUrls` is a custom
extension method defined in the same AppHost.cs file.

---

### 12. orleans-voting (`samples/orleans-voting/OrleansVoting.AppHost/apphost.ts`)

**Convertible:** Minimally  
**Status:** Redis works, but Orleans integration is entirely unavailable.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Redis | `AddRedis("voting-redis")` | ✅ Available |
| Orleans cluster | `AddOrleans("voting-cluster")` | ❌ Not available |
| Orleans clustering | `.WithClustering(redis)` | ❌ Not available |
| Orleans grain storage | `.WithGrainStorage("votes", redis)` | ❌ Not available |
| Project with replicas | `.WithReplicas(3)` | ✅ Available |
| Orleans reference | `.WithReference(orleans)` | ❌ Orleans resource not available |
| URL customization | `.WithUrlForEndpoint(...)` | ❌ Lambda callback not available |

**Key Limitation:** The entire `Aspire.Hosting.Orleans` package (AddOrleans, WithClustering,
WithGrainStorage) is not available in the polyglot SDK.

---

### 13. volume-mount (`samples/volume-mount/VolumeMount.AppHost/apphost.ts`)

**Convertible:** Partially  
**Status:** SQL Server works, but Azure Storage emulator does not.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| SQL Server + volume | `AddSqlServer("sqlserver").WithDataVolume()` | ✅ Available |
| Container lifetime | `.WithLifetime(ContainerLifetime.Persistent)` | ✅ Available |
| Add database | `.AddDatabase("sqldb")` | ✅ Available |
| Azure Storage emulator | `AddAzureStorage("Storage").RunAsEmulator(...)` | ❌ Not available |
| Emulator callback | `.RunAsEmulator(emulator => emulator.WithDataVolume())` | ❌ Lambda not available |
| Azure Blobs | `.AddBlobs("BlobConnection")` | ❌ Not available |
| Project reference | `AddProject<Projects.X>("blazorweb")` | ⚠️ `addProject("blazorweb")` |

**Key Limitation:** `AddAzureStorage` and `RunAsEmulator` with callback configuration are not
available. Azure storage integration is a major gap.

---

### 14. aspire-with-azure-functions (`samples/aspire-with-azure-functions/ImageGallery.AppHost/apphost.ts`)

**Convertible:** Minimally  
**Status:** Almost entirely blocked — Azure-specific APIs are not available.

| Feature | C# API | TypeScript Status |
|---------|--------|-------------------|
| Azure Container App Env | `AddAzureContainerAppEnvironment("env")` | ❌ Not available |
| Azure Storage | `AddAzureStorage("storage").RunAsEmulator()` | ❌ Not available |
| Configure infrastructure | `.ConfigureInfrastructure(...)` | ❌ Not available |
| URL display customization | `.WithUrls(...)` | ❌ Lambda callback not available |
| Azure Blobs | `storage.AddBlobs("blobs")` | ❌ Not available |
| Azure Queues | `storage.AddQueues("queues")` | ❌ Not available |
| Azure Functions project | `AddAzureFunctionsProject<Projects.X>("name")` | ❌ Not available |
| Role assignments | `.WithRoleAssignments(storage, ...)` | ❌ Not available |
| Host storage | `.WithHostStorage(storage)` | ❌ Not available |
| Project reference | `AddProject<Projects.X>("frontend")` | ⚠️ `addProject("frontend")` |

**Key Limitation:** This sample is entirely Azure-focused. None of the Azure-specific hosting
packages (Azure Storage, Functions, Container Apps) are available in the polyglot SDK.

---

## Cross-Cutting Issues Summary

### Universally Available Features ✅
These features work across all samples in the TypeScript polyglot SDK:
- `createBuilder()` — Create the distributed application builder
- `addRedis("name")` — Add Redis container resource
- `addPostgres("name")` — Add PostgreSQL container resource
- `addMySql("name")` — Add MySQL container resource
- `addSqlServer("name")` — Add SQL Server container resource
- `addContainer("name", "image", "tag")` — Add generic container resource
- `.addDatabase("name")` — Add database to a database server
- `.withDataVolume()` — Add data volume to container
- `.withLifetime(ContainerLifetime.Persistent)` — Set container lifetime
- `.withBindMount(source, target, isReadOnly)` — Add bind mount
- `.withEnvironment("key", "value")` — Set environment variable
- `.withArgs(...)` — Set container arguments
- `.withHttpEndpoint({ targetPort: N })` — Add HTTP endpoint
- `.withHttpHealthCheck("/path")` — Add HTTP health check
- `.withExternalHttpEndpoints()` — Expose endpoints externally
- `.withReference(resource)` — Add resource reference
- `.waitFor(resource)` — Wait for resource to be ready
- `.withReplicas(N)` — Set replica count
- `.withRedisCommander()` / `.withRedisInsight()` — Redis admin tools
- `.withPgAdmin()` — PostgreSQL admin tool
- `getEndpoint("name")` — Get endpoint reference
- `builder.executionContext` — Access execution context (run vs publish mode)
- `builder.build().run()` — Build and run the application

### Universally Unavailable Features ❌
These features are NOT available in any sample:
1. **`AddProject<Projects.X>("name")` generic type parameter** — The TypeScript SDK uses `addProject("name")` without type-safe project binding. This means the AppHost server needs another mechanism to discover and bind .NET project references.
2. **`WithUrlForEndpoint` with lambda callback** — URL display customization (display text, display location) requires callbacks that aren't available in the polyglot SDK.
3. **Custom C# extension methods** — Any extension method defined in the sample's AppHost project (e.g., `AddOpenTelemetryCollector`, `AddTalkingClock`, `WithFriendlyUrls`) requires `[AspireExport]` annotation and NuGet packaging to be available.

### Major Feature Category Gaps ❌
1. **JavaScript/Node.js hosting** (`Aspire.Hosting.JavaScript`): `AddJavaScriptApp`, `AddViteApp`, `AddNodeApp`, `WithNpm`, `WithRunScript`, `PublishAsDockerFile`
2. **Python hosting** (`Aspire.Hosting.Python`): `AddUvicornApp`, `WithUv`
3. **Azure integrations** (`Aspire.Hosting.Azure.*`): `AddAzureStorage`, `AddAzureFunctionsProject`, `AddAzureContainerAppEnvironment`, `RunAsEmulator`, `ConfigureInfrastructure`, `WithRoleAssignments`
4. **Orleans** (`Aspire.Hosting.Orleans`): `AddOrleans`, `WithClustering`, `WithGrainStorage`
5. **Docker Compose** (`Aspire.Hosting.Docker`): `AddDockerComposeEnvironment`
6. **Dockerfile builds**: `AddDockerfile`, `WithBuildArg`
7. **HealthChecks UI**: `AddHealthChecksUI`, `WithHttpProbe`
8. **Parameters**: `AddParameter` with `publishValueAsDefault`
9. **Completion waiting**: `WaitForCompletion` (different from `WaitFor`)
10. **Dashboard features**: `WithHttpCommand`, `WithHostPort`, `WithExplicitStart`, `ExcludeFromManifest`

### Sample Conversion Feasibility Matrix

| Sample | Feasibility | Notes |
|--------|-------------|-------|
| Metrics | ⚠️ Partial | Container resources work; custom OTel collector extension missing |
| aspire-shop | ✅ Mostly | Core orchestration works; HTTP commands, URL customization missing |
| aspire-with-javascript | ❌ Minimal | JS/Vite hosting entirely unavailable |
| aspire-with-node | ⚠️ Partial | Redis works; Node.js app hosting unavailable |
| aspire-with-python | ❌ Minimal | Python/Vite hosting entirely unavailable |
| client-apps-integration | ⚠️ Partial | API project works; platform checks, desktop apps unavailable |
| container-build | ❌ Minimal | Dockerfile builds entirely unavailable |
| custom-resources | ❌ None | Custom resources require C# implementation |
| database-containers | ✅ Mostly | Core DB hosting works; PgWeb, creation scripts missing |
| database-migrations | ✅ Mostly | Works except WaitForCompletion |
| health-checks-ui | ⚠️ Partial | Core projects work; Compose, HealthChecks UI, probes missing |
| orleans-voting | ❌ Minimal | Redis works; Orleans entirely unavailable |
| volume-mount | ⚠️ Partial | SQL Server works; Azure Storage unavailable |
| aspire-with-azure-functions | ❌ Minimal | Azure integrations entirely unavailable |

### Recommendations for Aspire Team

1. **Priority: Export JavaScript/Node.js hosting** — `AddJavaScriptApp`, `AddNodeApp`, `AddViteApp` should be annotated with `[AspireExport]` to enable polyglot samples involving JS frontends.
2. **Priority: Export Dockerfile builds** — `AddDockerfile` and `WithBuildArg` are fundamental container capabilities that should be available in all languages.
3. **Add `WithUrlForEndpoint` with simple string overload** — Instead of requiring a lambda, provide `withUrlForEndpoint("http", { displayText: "My App" })` as a simpler API.
4. **Export Azure integrations** — Azure Storage, Functions, and Container Apps are common enough to warrant ATS export.
5. **Add `WaitForCompletion`** — This is a commonly used capability for migration/init patterns.
6. **Document `addProject` limitations** — Clarify how TypeScript apphosts discover and reference .NET projects without generic type parameters.
7. **Support `AddParameter`** — Parameters with default values are important for publish-mode configuration.
8. **Consider custom resource extensibility** — Provide a mechanism for TypeScript apphosts to define custom resource types or interact with custom C# extensions.

---

## Environment and Testing Notes

### Running Polyglot AppHosts

To test any of these TypeScript apphosts:

```bash
# Install staging Aspire CLI
# On Windows:
iex "& { $(irm https://aspire.dev/install.ps1) } -Quality staging"

# On Linux/macOS:
curl -fsSL https://aspire.dev/install.sh | bash -s -- --quality staging

# Navigate to sample directory and run
cd samples/<sample-name>
aspire run
```

### Expected Behavior

When running `aspire run` with an `apphost.ts` present:
1. The CLI detects the TypeScript apphost
2. It scaffolds a .NET AppHost server project
3. It generates the TypeScript SDK in `.modules/`
4. It starts both the .NET server and Node.js guest
5. The Aspire dashboard shows all declared resources
6. Resources start in dependency order (via `waitFor`)

### Known Runtime Issues

1. **`.modules/` not pre-generated**: The TypeScript SDK is generated at runtime by the CLI. The
   `import ... from "./.modules/aspire.js"` will fail if run directly with `node` or `ts-node`.
   Always use `aspire run`.
2. **Project discovery**: `addProject("name")` may not automatically discover .NET project files.
   The CLI may need additional configuration or a manifest to map project names to paths.
3. **Async chaining**: The TypeScript SDK uses `Thenable` wrappers for fluent async chaining.
   Single `await` at the end of a chain is the expected pattern, but complex branching (like
   conditional `withDataVolume`) may require intermediate `await` calls.
