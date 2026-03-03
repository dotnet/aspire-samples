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
   Or on Linux/macOS:
   ```bash
   curl -fsSL https://aspire.dev/install.sh | bash -s -- --quality staging
   ```
2. Node.js (v18+) or Bun must be installed
3. **Add integration packages** using `aspire add {package}` for each sample (see per-sample setup below)
4. Run `aspire run` from the sample directory containing `apphost.ts`

### Adding Integrations with `aspire add`

The `aspire add` command adds NuGet hosting packages to the backing .NET AppHost server project.
This triggers code generation that makes the integration APIs available in the TypeScript SDK
(`.modules/aspire.js`). **You must run `aspire add` for each integration before the TypeScript
APIs become available.**

For example:
```bash
aspire add redis       # Makes builder.addRedis() available
aspire add postgres    # Makes builder.addPostgres() available
aspire add javascript  # Makes builder.addJavaScriptApp(), addNodeApp(), addViteApp() available
aspire add orleans     # Makes builder.addOrleans() available
```

The CLI will:
- Scaffold an AppHost server project (if not already present)
- Add the NuGet package to the server project
- Regenerate the TypeScript SDK in `.modules/` with the new capabilities
- Start the .NET AppHost server + Node.js/Bun guest runtime on `aspire run`

### Skipped Sample

- **standalone-dashboard**: This is a standalone console application, not an Aspire AppHost sample. It
  configures OpenTelemetry directly and does not use `DistributedApplication.CreateBuilder()`.

---

## Per-Sample Setup and Gap Analysis

Each sample requires specific `aspire add` commands to install its integration packages. Run these
commands from the sample directory before using `aspire run`.

### 1. Metrics (`samples/Metrics/MetricsApp.AppHost/apphost.ts`)

**Setup:** No additional packages required (uses core container and project APIs).

**Convertible:** Partially  
**Remaining Gaps:**
- `AddOpenTelemetryCollector(...)` — Custom C# extension method from `MetricsApp.AppHost.OpenTelemetryCollector`. Would need `[AspireExport]` and NuGet packaging.
- `.WithUrlForEndpoint` lambda callbacks — URL display text/location customization not available.

---

### 2. aspire-shop (`samples/aspire-shop/AspireShop.AppHost/apphost.ts`)

**Setup:**
```bash
aspire add postgres
aspire add redis
```

**Convertible:** Mostly  
**Remaining Gaps:**
- `.WithHttpCommand("/reset-db", ...)` — Custom dashboard commands not available.
- `.WithUrlForEndpoint` lambda callbacks — URL display text customization not available.

---

### 3. aspire-with-javascript (`samples/aspire-with-javascript/AspireJavaScript.AppHost/apphost.ts`)

**Setup:**
```bash
aspire add javascript
```

**Convertible:** Mostly (after `aspire add javascript`)  
**Remaining Gaps:**
- `.PublishAsDockerFile()` — Publish-time Dockerfile generation may not be available.
- `publishWithContainerFiles(reactVite, "./wwwroot")` — Bundling Vite output into a project's wwwroot may not be available.

---

### 4. aspire-with-node (`samples/aspire-with-node/AspireWithNode.AppHost/apphost.ts`)

**Setup:**
```bash
aspire add javascript
aspire add redis
```

**Convertible:** Fully (after `aspire add javascript` + `aspire add redis`)  
**Remaining Gaps:** None expected.

---

### 5. aspire-with-python (`samples/aspire-with-python/apphost.ts`)

**Setup:**
```bash
aspire add javascript
aspire add python
aspire add redis
```

**Convertible:** Mostly (after adding packages)  
**Remaining Gaps:**
- `publishWithContainerFiles(frontend, "./static")` — May not be available.

---

### 6. client-apps-integration (`samples/client-apps-integration/ClientAppsIntegration.AppHost/apphost.ts`)

**Setup:** No additional packages required.

**Convertible:** Mostly  
**Notes:** Uses `process.platform === "win32"` instead of `OperatingSystem.IsWindows()`.  
**Remaining Gaps:**
- `.withExplicitStart()` / `.excludeFromManifest()` — May not be available as capabilities.

---

### 7. container-build (`samples/container-build/apphost.ts`)

**Setup:** No additional packages required (uses core Dockerfile and parameter APIs).

**Convertible:** Mostly  
**Remaining Gaps:**
- `.withDeveloperCertificateTrust(true)` — Developer certificate trust may not be available.

---

### 8. custom-resources (`samples/custom-resources/CustomResources.AppHost/apphost.ts`)

**Setup:** N/A — This sample uses custom C# resource extensions (`AddTalkingClock`, `AddTestResource`).

**Convertible:** Not convertible  
**Remaining Gaps:**
- Custom resource types (`AddTalkingClock`, `AddTestResource`) are C# classes defined in the project. They would need `[AspireExport]` attributes and NuGet distribution to be accessible from TypeScript.

---

### 9. database-containers (`samples/database-containers/DatabaseContainers.AppHost/apphost.ts`)

**Setup:**
```bash
aspire add postgres
aspire add mysql
aspire add sqlserver
```

**Convertible:** Fully (after adding packages)  
**Notes:** Uses Node.js `readFileSync` to read `init.sql` and pass it to `withCreationScript()`.  
**Remaining Gaps:** None expected.

---

### 10. database-migrations (`samples/database-migrations/DatabaseMigrations.AppHost/apphost.ts`)

**Setup:**
```bash
aspire add sqlserver
```

**Convertible:** Fully (after adding package)  
**Notes:** Uses `waitForCompletion()` which should be available via core capabilities.  
**Remaining Gaps:** None expected.

---

### 11. health-checks-ui (`samples/health-checks-ui/HealthChecksUI.AppHost/apphost.ts`)

**Setup:**
```bash
aspire add redis
aspire add docker
```

**Convertible:** Mostly (after adding packages)  
**Remaining Gaps:**
- `.WithFriendlyUrls(...)` — Custom C# extension method defined in the AppHost project. Needs `[AspireExport]` and NuGet packaging.

---

### 12. orleans-voting (`samples/orleans-voting/OrleansVoting.AppHost/apphost.ts`)

**Setup:**
```bash
aspire add redis
aspire add orleans
```

**Convertible:** Mostly (after adding packages)  
**Remaining Gaps:**
- `.WithUrlForEndpoint` lambda callbacks — URL display text/location customization not available.

---

### 13. volume-mount (`samples/volume-mount/VolumeMount.AppHost/apphost.ts`)

**Setup:**
```bash
aspire add sqlserver
aspire add azure-storage
```

**Convertible:** Fully (after adding packages)  
**Remaining Gaps:** None expected.

---

### 14. aspire-with-azure-functions (`samples/aspire-with-azure-functions/ImageGallery.AppHost/apphost.ts`)

**Setup:**
```bash
aspire add azure-appcontainers
aspire add azure-storage
aspire add azure-functions
```

**Convertible:** Mostly (after adding packages)  
**Remaining Gaps:**
- `.ConfigureInfrastructure(...)` — Bicep infrastructure configuration using C# lambdas is not directly available. Default settings will be used.
- `.WithUrlForEndpoint` lambda callbacks — URL display text customization not available.

---

## Cross-Cutting Issues Summary

### Features Available After `aspire add` ✅
These features work after adding the appropriate integration packages:
- `createBuilder()` — Create the distributed application builder (core)
- `addRedis("name")` — `aspire add redis`
- `addPostgres("name")` / `.withPgAdmin()` / `.withPgWeb()` — `aspire add postgres`
- `addMySql("name")` — `aspire add mysql`
- `addSqlServer("name")` — `aspire add sqlserver`
- `addJavaScriptApp()` / `addNodeApp()` / `addViteApp()` — `aspire add javascript`
- `addUvicornApp()` / `.withUv()` — `aspire add python`
- `addOrleans()` / `.withClustering()` / `.withGrainStorage()` — `aspire add orleans`
- `addDockerComposeEnvironment()` — `aspire add docker`
- `addHealthChecksUI()` / `.withHttpProbe()` — `aspire add docker` (or dedicated package)
- `addAzureStorage()` / `.addBlobs()` / `.addQueues()` — `aspire add azure-storage`
- `addAzureContainerAppEnvironment()` — `aspire add azure-appcontainers`
- `addAzureFunctionsProject()` — `aspire add azure-functions`
- Core capabilities (always available): `addContainer()`, `addProject()`, `addDockerfile()`, `addParameter()`, `.withBindMount()`, `.withEnvironment()`, `.withArgs()`, `.withHttpEndpoint()`, `.withHttpHealthCheck()`, `.withExternalHttpEndpoints()`, `.withReference()`, `.waitFor()`, `.waitForCompletion()`, `.withReplicas()`, `.withDataVolume()`, `.withLifetime()`, `.withOtlpExporter()`, `.withBuildArg()`, `getEndpoint()`, `builder.executionContext`, `builder.build().run()`

### Remaining Gaps ❌
These features have no polyglot equivalent regardless of packages:
1. **`.WithUrlForEndpoint` with lambda callback** — URL display customization (display text, display location) requires C# callbacks that can't be expressed in TypeScript.
2. **`.ConfigureInfrastructure` with lambda** — Bicep infrastructure configuration requires C# lambdas for accessing provisioning types.
3. **Custom C# extension methods** — Any extension method defined in the sample's AppHost project (e.g., `AddOpenTelemetryCollector`, `AddTalkingClock`, `WithFriendlyUrls`) requires `[AspireExport]` annotation and NuGet packaging.
4. **`.WithHttpCommand`** — Custom dashboard commands are not exposed through ATS capabilities.
5. **`.PublishAsDockerFile` / `.publishWithContainerFiles`** — Publish-time behaviors may not be available.

### Sample Conversion Feasibility Matrix (with `aspire add`)

| Sample | `aspire add` Commands | Feasibility | Remaining Gaps |
|--------|----------------------|-------------|----------------|
| Metrics | (none) | ⚠️ Partial | Custom OTel collector extension, URL callbacks |
| aspire-shop | `postgres`, `redis` | ✅ Mostly | HTTP commands, URL callbacks |
| aspire-with-javascript | `javascript` | ✅ Mostly | `PublishAsDockerFile`, `publishWithContainerFiles` |
| aspire-with-node | `javascript`, `redis` | ✅ Full | — |
| aspire-with-python | `javascript`, `python`, `redis` | ✅ Mostly | `publishWithContainerFiles` |
| client-apps-integration | (none) | ✅ Mostly | `withExplicitStart`, `excludeFromManifest` |
| container-build | (none) | ✅ Mostly | `withDeveloperCertificateTrust` |
| custom-resources | N/A | ❌ None | Custom C# resource types |
| database-containers | `postgres`, `mysql`, `sqlserver` | ✅ Full | — |
| database-migrations | `sqlserver` | ✅ Full | — |
| health-checks-ui | `redis`, `docker` | ✅ Mostly | Custom `WithFriendlyUrls` extension |
| orleans-voting | `redis`, `orleans` | ✅ Mostly | URL callbacks |
| volume-mount | `sqlserver`, `azure-storage` | ✅ Full | — |
| aspire-with-azure-functions | `azure-appcontainers`, `azure-storage`, `azure-functions` | ✅ Mostly | `ConfigureInfrastructure` lambda, URL callbacks |

### Recommendations

1. **Add `[AspireExport]` to `WithUrlForEndpoint`** — Provide a non-lambda overload (e.g., `withUrlForEndpoint("http", { displayText: "My App" })`) for display customization.
2. **Add `[AspireExport]` to `WithHttpCommand`** — Dashboard commands are useful for operations like database resets.
3. **Document `addProject` behavior** — Clarify how TypeScript apphosts discover and reference .NET projects without generic type parameters.
4. **Consider custom resource extensibility** — Provide a mechanism for TypeScript apphosts to interact with custom C# extensions that have `[AspireExport]` attributes.

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

# Navigate to sample directory
cd samples/<sample-name>

# Add required integration packages (see per-sample setup above)
aspire add redis
aspire add postgres
# ... etc.

# Run the polyglot apphost
aspire run
```

### Expected Behavior

When running `aspire run` with an `apphost.ts` present:
1. The CLI detects the TypeScript apphost
2. It scaffolds a .NET AppHost server project (if not already present)
3. `aspire add` installs NuGet packages and triggers SDK regeneration
4. It generates the TypeScript SDK in `.modules/` with all available capabilities
5. It starts both the .NET server and Node.js guest
6. The Aspire dashboard shows all declared resources
7. Resources start in dependency order (via `waitFor`)

### Known Runtime Issues

1. **`.modules/` not pre-generated**: The TypeScript SDK is generated at runtime by the CLI. The
   `import ... from "./.modules/aspire.js"` will fail if run directly with `node` or `ts-node`.
   Always use `aspire run`.
2. **Must run `aspire add` first**: Integration APIs (like `addRedis`, `addPostgres`) are only
   available after adding the corresponding packages with `aspire add`. Without them, the generated
   SDK won't include those capabilities.
3. **Project discovery**: `addProject("name")` discovers .NET projects via the Aspire CLI's
   project detection. Ensure project files are in the expected directory structure.
4. **Async chaining**: The TypeScript SDK uses `Thenable` wrappers for fluent async chaining.
   Single `await` at the end of a chain is the expected pattern, but complex branching (like
   conditional `withDataVolume`) may require intermediate `await` calls.
