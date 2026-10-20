# Copilot Instructions for aspire-samples

## Repository Overview

This is the official .NET Aspire samples repository (`dotnet/aspire-samples`). It contains independent sample applications demonstrating Aspire features — each under `samples/<SampleName>/` with its own `.sln`. The repo targets .NET SDK 10.0.100 (see `global.json`) and many samples are in the process of being upgraded from .NET 8 / Aspire 8 to .NET 10 / Aspire 13.4+.

## Build and Test

Build all samples (installs Aspire workload first):

```shell
# Windows
.\build.cmd

# Linux/macOS
./build.sh
```

Build a single sample:

```shell
dotnet build samples/<SampleName>/<SampleName>.sln
```

Run integration tests (all samples):

```shell
dotnet test ./tests/SamplesTests.sln
```

Run a single test by name:

```shell
dotnet test ./tests/SamplesTests.sln --filter "AppHostRunsCleanly(AspireShop.AppHost.dll)"
```

Run a single sample's AppHost:

```shell
cd samples/<SampleName>/<SampleName>.AppHost
dotnet run
```

## Sample Architecture

Each sample follows a consistent Aspire project structure:

- **`<Name>.AppHost/`** — The Aspire orchestrator. Contains `Program.cs` (or `AppHost.cs` in upgraded samples) that wires resources together using `DistributedApplication.CreateBuilder`. The AppHost references all other projects in the sample.
- **`<Name>.ServiceDefaults/`** — Shared project providing OpenTelemetry, health checks, service discovery, and HTTP resilience to all services. Marked with `<IsAspireSharedProject>true</IsAspireSharedProject>`.
- **Service/API projects** — Individual ASP.NET Core services that call `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`.
- **`samples/Shared/`** — Utilities shared across multiple samples (e.g., `ParameterExtensions.cs` for stable passwords).

## Key Conventions

### License Headers

Files under `tests/` and some under `samples/` use the .NET Foundation license header:

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
```

### Integration Tests

Tests in `tests/SamplesIntegrationTests/` use `Aspire.Hosting.Testing` with xUnit. They dynamically discover all `*.AppHost.dll` assemblies at runtime — the test project references every AppHost via a wildcard `<ProjectReference Include="../../samples/**/*.AppHost/*.AppHost.csproj" />`.

Two test patterns exist:
1. **`AppHostRunsCleanly`** — Starts each AppHost and asserts no errors in logs.
2. **`TestEndpointsReturnOk`** — Starts an AppHost, hits specific HTTP endpoints, and asserts `200 OK`. Test endpoints are defined inline in `AppHostTests.cs` — when adding a new sample, add its endpoints there.

### Upgrading Samples to Aspire 13.4+

A detailed upgrade skill is available at `.github/skills/aspire-upgrade/SKILL.md`. Key changes when upgrading from Aspire 8 → 13.4:

- Remove `<IsAspireHost>true</IsAspireHost>` from AppHost `.csproj`
- Add `<Sdk Name="Aspire.AppHost.Sdk" Version="13.4.3" />` to AppHost `.csproj`
- Remove the `Aspire.Hosting.AppHost` PackageReference (now provided by the SDK)
- Change `<TargetFramework>` from `net8.0` to `net10.0` in all `.csproj` files
- Update all Aspire hosting/client NuGet packages to 13.4.x
- Update OpenTelemetry packages to latest stable versions
- Rename `Program.cs` → `AppHost.cs` in AppHost projects (Aspire 13.4 convention)

### Code Style

Enforced via `.editorconfig`:
- 4-space indentation for C# files (2-space for XML/project files)
- `var` preferred; braces preferred
- PascalCase for constants
- Allman-style braces (new line before `{`)
- UTF-8 with BOM for C# files

### MCP Configuration

The repo includes an Aspire MCP server configuration at `.vscode/mcp.json`, enabling Aspire tooling integration in VS Code.
