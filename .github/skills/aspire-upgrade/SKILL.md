---
name: aspire-upgrade
description: >
  Upgrade Aspire sample applications in this repository from older versions (.NET 8 / Aspire 8)
  to modern versions (.NET 10 / Aspire 13.4+). Use this skill when asked to upgrade, update, or
  migrate an Aspire sample to a newer .NET or Aspire version.
---

# Aspire Upgrade Skill (Project)

This skill guides the upgrade of Aspire sample applications in the aspire-samples repository.

## Pre-flight

1. **Check installed .NET SDK**: Run `dotnet --list-sdks` to confirm the target SDK is available.
2. **Identify the sample**: Each sample lives under `samples/<SampleName>/` with its own `.sln`.
3. **Check current versions**: Inspect `global.json`, all `.csproj` files, and source files to understand the starting point.
4. **Look up latest package versions**: Query NuGet (e.g. via the flatcontainer API) for the latest stable versions of all packages before making changes.

## Upgrade Steps

### 1. `global.json`

Update the SDK version to match the target .NET version:

```json
{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }
```

### 2. Target Framework Moniker

Change `<TargetFramework>` in **every** `.csproj` in the sample:

- `net8.0` → `net10.0`

### 3. AppHost Project (`.AppHost.csproj`)

The AppHost project requires the most changes:

- **Remove** `<IsAspireHost>true</IsAspireHost>` — this triggers a deprecated workload error in .NET 10.
- **Add** `<Sdk Name="Aspire.AppHost.Sdk" Version="13.4.3" />` as a child of `<Project>`.
- **Remove** the `Aspire.Hosting.AppHost` PackageReference — it is now provided by the SDK.
- **Update** hosting integration packages (e.g. `Aspire.Hosting.Azure.CosmosDB`) to 13.4.x.

Example result:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Aspire.AppHost.Sdk" Version="13.4.3" />
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  ...
</Project>
```

### 4. AppHost Source Code

Watch for renamed APIs when upgrading hosting integrations:

| Aspire 8 API | Aspire 13.4 API |
|---|---|
| `.AddDatabase("name")` | `.AddCosmosDatabase("name")` |

Also check that fluent call chains are valid. For example, `RunAsEmulator()` should be called on the Cosmos DB account resource, not on a database resource.

When a resource is passed to another project using the `WithReference()` method, use the `WaitFor()` method to ensure the resource is fully provisioned before it is referenced.

### 5. AppHost File Naming

In Aspire 13.4, the convention is to name the AppHost entry point `AppHost.cs` rather than `Program.cs`. Rename if requested — the .NET SDK compiles all `.cs` files automatically so no other references need updating.

### 6. ServiceDefaults Project

Update all NuGet packages:

| Package | Aspire 8 Version | Aspire 13.4 Version |
|---|---|---|
| `Microsoft.Extensions.Http.Resilience` | 8.x | 10.0.0 |
| `Microsoft.Extensions.ServiceDiscovery` | 8.x | 10.0.0 |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.9.x | 1.15.3 |
| `OpenTelemetry.Extensions.Hosting` | 1.9.x | 1.15.3 |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.9.x | 1.15.2 |
| `OpenTelemetry.Instrumentation.Http` | 1.9.x | 1.15.1 |
| `OpenTelemetry.Instrumentation.Runtime` | 1.9.x | 1.15.1 |

**Note**: The OpenTelemetry instrumentation packages have different latest versions from each other — always check NuGet for the actual latest stable version of each package individually.

### 7. ServiceDefaults `Extensions.cs`

Refresh to match the Aspire 13.4 template:

- Use generic type parameter `TBuilder` instead of concrete `IHostApplicationBuilder`.
- Add `AddSource(builder.Environment.ApplicationName)` to tracing.
- Add health endpoint path filtering to exclude `/health` and `/alive` from traces.
- Use constants for health endpoint paths.

### 8. OpenAPI / Swagger Migration

If the sample uses NSwag:

- **Remove** `NSwag.AspNetCore` package reference.
- **Update** `Microsoft.AspNetCore.OpenApi` to the .NET 10 version (e.g. `10.0.8`).
- **Replace** `AddEndpointsApiExplorer()` + `AddOpenApiDocument()` → `AddOpenApi()`.
- **Replace** `UseOpenApi()` + `UseSwaggerUi()` → `MapOpenApi()`.
- **Update** `launchSettings.json`: change `"launchUrl": "swagger"` → `"launchUrl": "openapi/v1.json"`.

### 9. Other Client Packages

Update remaining NuGet packages to their latest stable versions:

- `Aspire.Microsoft.Azure.Cosmos` → 13.4.3
- `Refit` → check latest (e.g. 11.0.1)
- Any other third-party packages

### 10. Aspire using Hosting

The `using Aspire.Hosting;` directive in the AppHost `Program.cs`/`AppHost.cs` can be removed if no longer needed — the Aspire SDK imports these namespaces automatically. Check whether it's still required after the upgrade.

### 11. Check launch profiles for all projects

Make sure the `launchSettings.json` files in all projects are updated to reflect any changes in endpoints, ports, or launch URLs.

## Verification

1. **Build**: Run `dotnet build <SampleName>.sln` and confirm 0 errors and 0 warnings.
2. **Run**: Start the AppHost with `dotnet run` and check the Aspire dashboard.
3. **Check resources**: Verify all resources (emulators, services) start and reach a healthy state.
4. **Check structured logs**: Look for success messages (e.g. "Database successfully created!").
5. **Check endpoints**: Verify the dashboard URLs point to valid endpoints (no stale `/swagger` paths).
