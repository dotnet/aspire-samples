---
languages:
- csharp
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire and Functions image gallery sample"
urlFragment: "aspire-azure-functions-with-blob-triggers"
description: "An example image gallery app written with .NET Aspire and Azure Functions."
---

# Image Gallery

The app consists of two services:

- **ImageGallery.Frontend**: This is a Blazor app that displays a for uploading of images, showing thumbnails of images in a grid.
- **ImageGallery.Functions**: This is an Azure Function triggered by the arrival of a new blob using a Functions Blob Trigger.

The app also includes a class library project, **ImageGallery.ServiceDefaults**, that contains the service defaults used by the service projects, and the **ImageGallery.AppHost** Aspire App Host project.

## Pre-requisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Azure Functions tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=windows%2Cisolated-process%2Cnode-v4%2Cpython-v2%2Chttp-trigger%2Ccontainer-apps&pivots=programming-language-csharp)
- **Optional** [Visual Studio 2022 17.12](https://visualstudio.microsoft.com/vs/preview/)

## Running the app

If using Visual Studio, open the solution file `ImageGallery.sln` and launch/debug the `ImageGallery.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `ImageGallery.AppHost` directory.
