# eShop *Lite*

This sample is a simplified version of the [eShop sample application](https://github.com/dotnet/eshop), including a small subset of the original features and a simplified architecture.

The app consists of four services:

- **eShopLite.Frontend**: This is a Blazor app that displays a paginated catlog of products and allows users to add products to a shopping cart.
- **eShopLite.CatalogService**: This is an HTTP API that provides access to the catalog of products stored in a PostgreSQL database.
- **eShopLite.CatalogDb**: This is an HTTP API that manages the initialization and updating of the catalog database.
- **eShopLite.BasketService**: This is a gRPC service that provides access to the shopping cart stored in Redis.

The app also includes a class library project, **eShopLite.ServiceDefaults**, that contains the service defaults used by the service projects.

## Pre-requisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Optional** [Visual Studio 2022 17.9 Preview](https://visualstudio.microsoft.com/vs/preview/)

## Running the app

If using Visual Studio, open the solution file `eShopLite.sln` and launch/debug the `eShopLite.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `eShopLite.AppHost` directory.

## Deploying the app to Azure Container Apps using Azure Command-Line Interface (CLI)

Read the [deployment guide](./deploy-az-cli.cmd) for details on how to deploy the app to Azure Container Apps using the Azure CLI.
