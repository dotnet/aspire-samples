---
languages:
- csharp
products:
- dotnet
- dotnet-orleans
- dotnet-aspire
page_type: sample
name: "Orleans Voting sample app on Aspire"
urlFragment: "orleans-voting-sample-app-on-aspire"
description: "An Orleans sample demonstrating a voting app on Aspire."
---

# .NET Aspire Orleans sample app

This is a simple .NET app that shows how to use Orleans with .NET Aspire orchestration.

## Demonstrates

- How to configure a .NET Aspire app to work with Orleans

## Sample prerequisites

This sample is written in C# and targets .NET 8.0. It requires the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.

## Building the sample

To download and run the sample, follow these steps:

1. Clone the `dotnet/aspire-samples` repository.
2. In Visual Studio (2022 or later):
    1. On the menu bar, choose **File** > **Open** > **Project/Solution**.
    2. Navigate to the folder that holds the sample code, and open the solution (.sln) file.
    3. Right click the _OrleansVoting.AppHost_ project in the solution explore and choose it as the startup project.
    4. Choose the <kbd>F5</kbd> key to run with debugging, or <kbd>Ctrl</kbd>+<kbd>F5</kbd> keys to run the project without debugging.
3. From the command line:
   1. Navigate to the folder that holds the sample code.
   2. At the command line, type [`dotnet run`](https://docs.microsoft.com/dotnet/core/tools/dotnet-run).

To run the game, run the .NET Aspire app by executing the following at the command prompt (opened to the base directory of the sample):

``` bash
dotnet run --project OrleansVoting.AppHost
```

1. On the **Resources** page, click on one of the endpoints for the listed project. This launches the simple .NET app.
2. In the .NET app:
    1. Enter a poll title, some questions, and click **Create**, *or* click **DEMO: auto-fill poll** to auto-fill the poll.
    2. On the poll page, Click one of the poll options to vote for it.
    3. The results of the poll are displayed. Click the **DEMO: simulate other voters** button to simulate other voters voting on the poll and watch the results update.

For more information about using Orleans, see the [Orleans documentation](https://learn.microsoft.com/dotnet/orleans).

