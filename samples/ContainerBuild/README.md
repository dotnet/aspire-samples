---
languages:
- csharp
- go
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire Container Build Sample"
urlFragment: "aspire-container-build"
description: "An example of integrating apps written in other languages using container-based builds in a .NET Aspire app."
---

# Working with container-built resources in a .NET Aspire application

This sample demonstrates integrating applications into a .NET Aspire app via Dockerfiles and container-based builds. This is especially helpful to integrate applications written in languages that .NET Aspire does not have a native integration for, or to reduce the prerequisites required to run the application.

![Screenshot of the Aspire dashboard showing the ginapp container resource built from a Dockerfile](./images/aspire-dashboard-container-build.png)

The sample integrates a simple app written using [Go](https://go.dev/) and the [Gin Web Framework](https://gin-gonic.com/) by using a [Dockerfile](./ginapp/Dockerfile):

- **ginapp**: This is a simple "Hello, World" HTTP API that returns a JSON object like `{ "message": "Hello, World!" }` from `/`.

## Pre-requisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [A container runtime supported by .NET Aspire, e.g. Docker Desktop or Podman](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling#container-runtime)
- **Optional** [Visual Studio 2022 17.12](https://visualstudio.microsoft.com/vs/)

## Running the app

If using Visual Studio, open the solution file `ContainerBuild.sln` and launch/debug the `ContainerBuild.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `ContainerBuild.AppHost` directory.

From the Aspire dashboard, click on the endpoint URL for the `ginapp` resource to see the response in the browser.
