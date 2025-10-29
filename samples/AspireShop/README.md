---
languages:
- csharp
products:
- dotnet
- dotnet-aspire
page_type: sample
name: "Aspire Shop sample"
urlFragment: "aspire-shop"
description: "An example shop app written with Aspire."
---

# Aspire Shop

![Screenshot of the web front end the .Aspire Shop sample](./images/aspireshop-frontend-complete.png)

The app consists of four services:

- **AspireShop.Frontend**: This is a Blazor app that displays a paginated catlog of products and allows users to add products to a shopping cart.
- **AspireShop.CatalogService**: This is an HTTP API that provides access to the catalog of products stored in a PostgreSQL database.
- **AspireShop.CatalogDbManager**: This is an HTTP API that manages the initialization and updating of the catalog database.
- **AspireShop.BasketService**: This is a gRPC service that provides access to the shopping cart stored in Redis.

The app also includes a class library project, **AspireShop.ServiceDefaults**, that contains the service defaults used by the service projects.

## Pre-requisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Optional** [VS Code](https://code.visualstudio.com/)
- **Optional** [Visual Studio 2026](https://visualstudio.microsoft.com/vs/preview/)

## Running the app

If using the Aspire CLI, run `aspire run` from this directory.

If using VS Code, open this directory as a workspace.

If using Visual Studio, open the solution file `AspireShop.slnx` and launch/debug the `AspireShop.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `AspireShop.AppHost` directory.
