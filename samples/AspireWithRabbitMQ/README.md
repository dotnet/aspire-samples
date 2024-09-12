---
languages:
- csharp
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire and RabbitMQ Integration sample"
urlFragment: "aspire-rabbitmq"
description: "This is a sample project using the .NET Aspire RabbitMQ client as a message broker"
---

# .NET Aspire with RabbitMQ 

![Screenshot of the Sender and Receiver testing](./images/sender-receiever-testing.png)

This sample demonstrates an approach for integrating a RabbitMQ into a .NET Aspire application.

The project consists of two services:

- **AspireWithRabbitMQ.Sender**: This is a minimal api project for sending events/messages to RabbitMQ.
- **AspireWithRabbitMQ.Receiver**: Minimal api project for retrieving events/messages from RabbitMQ.

## Pre-requisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Optional** [Visual Studio 2022 17.10](https://visualstudio.microsoft.com/vs/preview/)

## Running the app

If using Visual Studio, open the solution file `AspireWithRabbitMQ.sln` and launch/debug the `AspireWithRabbitMQ.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `AspireWithRabbitMQ.AppHost` directory or `dotnet run --project .\AspireWithRabbitMQ.AppHost\` from the root directory.
