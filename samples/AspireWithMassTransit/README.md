---
languages:
- csharp
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire MassTransit sample"
urlFragment: "aspire-dapr"
description: "An example of how to integrate MassTransit into a .NET Aspire app."
---

# Integrating MassTransit into a .NET Aspire application

This sample demonstrates an approach for integrating [MassTransit](https://MassTransit.io/) into a .NET Aspire application.

## Pre-requisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Optional** [Visual Studio 2022 17.9 Preview](https://visualstudio.microsoft.com/vs/preview/)

## Running the app

If using Visual Studio, open the solution file `AspireWithMassTransit.sln` and launch/debug the `AspireWithMassTransit.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `AspireWithMassTransit.AppHost` directory.

## Experiencing the app

Once the app is running, the .NET Aspire dashboard will launch in your browser:

Navigate to https://localhost:17131 in the browser to publish some messages.

After that check the log. Consumer should received and consume the messages.

```
2024-04-18T20:37:35.2219340 info: AspireWithMassTransit.ApiService.HelloAspireEventConsumer[0]
      Received: Hello, .NET!
2024-04-18T20:37:35.2219350 info: AspireWithMassTransit.ApiService.HelloAspireEventConsumer[0]
      Received: Hello, Aspire!