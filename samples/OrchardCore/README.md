---
languages:
- csharp
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire OrchardCore sample app"
urlFragment: "aspire-orchard-core"
description: "A sample .NET Aspire app that shows how to use OrchardCore"
---

# .NET Aspire OrchardCore CMS sample app

This is a simple .NET app that shows how to use OrchardCore with .NET Aspire orchestration.

## Demonstrates

- How to configure a .NET Aspire app to work with OrchardCore

## Sample prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Optional** [Visual Studio 2022 17.9 Preview](https://visualstudio.microsoft.com/vs/preview/)


## Running the sample

To download and run the sample, follow these steps:

### Run the project using Visual Studio

To run the sample project using Visual Studio, open Visual Studio (2022 or later), then:

    1. On the menu bar, choose **File** > **Open** > **Project/Solution**.
    2. Navigate to the folder that holds the unzipped sample code, and open the solution (.sln) file.
    3. Right click the _Aspire.AppHost_ project in the solution explore and choose it as the startup project.
    4. Choose the <kbd>F5</kbd> key to run with debugging, or <kbd>Ctrl</kbd>+<kbd>F5</kbd> keys to run the project without debugging.

### Run the project using command line

To run the .NET Aspire app, open a command line console and change directory to the OrchardCore solution folder. Then execute the following command:

``` bash
dotnet run --project Aspire/Aspire.AppHost
```

On the **Projects** page, click on one of the endpoints (OrchardCore CMS) for the listed project. This launches the simple .NET app.

For more information about using OrchardCore, see the [OrchardCore documentation](https://docs.orchardcore.net/en/latest/).
