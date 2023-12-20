---
languages:
- csharp
- sql
- bash
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire Database Containers sample"
urlFragment: "aspire-database-containers"
description: "An example of working with database containers in a .NET Aspire app."
---

# Working with database containers in a .NET Aspire application

This sample demonstrates working with database containers in a .NET Aspire app, using the features of the underlying container image to modify the default database created during container startup. This is especially helpful when not using an ORM like Entity Framework Core that can run migrations on application startup (e.g., [as in the eShopLite sample](../eShopLite/eShopLite.CatalogDbManager)) and handle cases when the database configured in the AppHost is not yet created.

![Screenshot of the Swagger UI for the API service that returns data from the configured database containers](./images/db-containers-apiservice-swagger-ui.png)

The app uses the following database container types:

- [Microsoft SQL Server](https://mcr.microsoft.com/en-us/product/mssql/server/about)
- [MySQL](https://hub.docker.com/_/mysql)
- [PostgreSQL](https://hub.docker.com/_/postgres/)

The app consists of an API service:

- **ContainerDatabases.ApiService**: This is an HTTP API that returns data from each of the configured databases.

## Pre-requisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Optional** [Visual Studio 2022 17.9 Preview](https://visualstudio.microsoft.com/vs/preview/)

## Running the app

If using Visual Studio, open the solution file `DatabaseContainers.sln` and launch/debug the `DatabaseContainers.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `DatabaseContainers.AppHost` directory.

From the Aspire dashboard, click on the endpoint URL for the `ContainerDatabases.ApiService` project to launch the Swagger UI for the APIs. You can use the UI to call the APIs and see the results.