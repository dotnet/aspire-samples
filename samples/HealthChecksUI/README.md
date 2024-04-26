---
languages:
- csharp
- sql
- bash
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire ASP.NET Core HealthChecksUI sample"
urlFragment: "aspire-health-checks-ui"
description: "An example of running the ASP.NET Core HealthChecksUI container in a .NET Aspire app."
---

# Configuring health checks & running the ASP.NET Core HealthChecksUI container in a .NET Aspire application

This sample demonstrates running the [ASP.NET Core HealthChecksUI container](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/blob/master/doc/ui-docker.md) in a .NET Aspire app.

![Screenshot of the HealthChecksUI](./images/healthchecksui.png)

The app is based on the .NET Aspire Starter App project template and thus consists of a frontend Blazor app that communicates with a backend ASP.NET Core API service and a Redis cache.

## Pre-requisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An [Aspire-supported container runtime](https://aka.ms/dotnet/aspire/containers)
- **Optional** [Visual Studio 2022 17.10 Preview](https://visualstudio.microsoft.com/vs/preview/)

## Running the app

If using Visual Studio, open the solution file `HealthChecksUI.sln` and launch/debug the `HealthChecksUI.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `HealthChecksUI.AppHost` directory.

From the Aspire dashboard, click on the endpoint URL for the `healthchecksui` resource to launch the HealthChecksUI.

## Implementation details

TODO