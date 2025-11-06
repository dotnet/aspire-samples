---
languages:
- csharp
- javascript
products:
- dotnet
- dotnet-aspire
page_type: sample
name: "Aspire Node.js App sample"
urlFragment: "aspire-nodejs"
description: "An example of integrating a Node.js app and an ASP.NET Core HTTP API using Aspire."
---

# Integrating a Node.js app within an Aspire application

This sample demonstrates integrating a Node.js app and an ASP.NET Core HTTP API using Aspire.

The sample consists of two apps:

- **NodeFrontend**: This is a simple Express-based Node.js app that renders a table of weather forecasts retrieved from a backend API and utilizes a Redis cache.
- **AspireWithNode.AspNetCoreApi**: This is an HTTP API that returns randomly generated weather forecast data.

## Prerequisites

- [Aspire development environment](https://aspire.dev/get-started/prerequisites/)
- [Node.js](https://nodejs.org) - at least version 24.11.0
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Running the app

If using the Aspire CLI, run `aspire run` from this directory.

If using VS Code, open this directory as a workspace and launch the `AspireWithNode.AppHost` project using either the Aspire or C# debuggers.

If using Visual Studio, open the solution file `AspireWithNode.slnx` and launch/debug the `AspireWithNode.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `AspireWithNode.AppHost` directory.
