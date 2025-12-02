# Image Gallery

![Screenshot of the web frontend the Aspire with Azure Functions sample](./images/aspire-with-functions.png)

The app consists of two services:

- **ImageGallery.Frontend**: This is a Blazor app that displays a for uploading of images, showing thumbnails of images in a grid.
- **ImageGallery.Functions**: This is an Azure Function triggered by the arrival of a new blob using a Functions Blob Trigger.

The app also includes a class library project, **ImageGallery.ServiceDefaults**, that contains the service defaults used by the service projects, and the **ImageGallery.AppHost** Aspire App Host project.

## Pre-requisites

- [Aspire development environment](https://aspire.dev/get-started/prerequisites/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=windows%2Cisolated-process%2Cnode-v4%2Cpython-v2%2Chttp-trigger%2Ccontainer-apps&pivots=programming-language-csharp)

## Running the app

If using the Aspire CLI, run `aspire run` from this directory.

If using VS Code, open this directory as a workspace and launch the `ImageGallery.AppHost` project using either the Aspire or C# debuggers.

If using Visual Studio, open the solution file `ImageGallery.slnx` and launch/debug the `ImageGallery.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `ImageGallery.AppHost` directory.
