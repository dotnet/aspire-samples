---
languages:
- csharp
- javascript
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire Node.js App sample"
urlFragment: "aspire-nodejs"
description: "An example of how to integrate a Node.js app into a .NET Aspire app."
---

# Integrating a Node.js app within a .NET Aspire application

This sample demonstrates an approach for integrating a Node.js app into a .NET Aspire application.

The app consists of two services:

- **NodeFrontend**: This is a simple Express-based Node.js app that renders a table of weather forecasts retrieved from a backend API and utilizes a Redis cache.
- **AspireWithNode.AspNetCoreApi**: This is an HTTP API that returns randomly generated weather forecast data.

## Pre-requisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js](https://nodejs.org) - at least version 20.9.0
- **Optional** [Visual Studio 2022 17.9 Preview](https://visualstudio.microsoft.com/vs/preview/)

## Running the app

If using Visual Studio, open the solution file `AspireWithNode.sln` and launch/debug the `AspireWithNode.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `AspireWithNode.AppHost` directory.
