---
languages:
- csharp
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire metrics sample app"
urlFragment: "aspire-metrics"
description: "A sample .NET Aspire app that collects and displays metrics using Prometheus and Grafana."
---

# .NET Aspire metrics sample app

This is a simple .NET app that shows off collecting metrics with OpenTelemetry and exporting them to Prometheus and Grafana for reporting.

![Screenshot of the ASP.NET Core Grafana dashboard](./images/dashboard-screenshot.png)

## Demonstrates

- How to configure a .NET Aspire app to export metrics to Prometheus
- How to add Prometheus and Grafana containers to a .NET Aspire app
- How to configure Prometheus and Grafana to collect and display metrics in the [.NET Grafana dashboard](https://aka.ms/dotnet/grafana-source)

## Sample prerequisites

This sample is written in C# and targets .NET 8.0. It requires the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.

## Building the sample

To download and run the sample, follow these steps:

1. Download and unzip the sample.
2. In Visual Studio (2022 or later):
    1. On the menu bar, choose **File** > **Open** > **Project/Solution**.
    2. Navigate to the folder that holds the unzipped sample code, and open the solution (.sln) file.
    3. Right click the _MetricsApp.AppHost_ project in the solution explore and choose it as the startup project.
    4. Choose the <kbd>F5</kbd> key to run with debugging, or <kbd>Ctrl</kbd>+<kbd>F5</kbd> keys to run the project without debugging.
3. From the command line:
   1. Navigate to the folder that holds the unzipped sample code.
   2. At the command line, type [`dotnet run`](https://docs.microsoft.com/dotnet/core/tools/dotnet-run).

To run the game, run the .NET Aspire app by executing the following at the command prompt (opened to the base directory of the sample):

``` bash
dotnet run --project MetricsApp.AppHost
```

1. On the **Projects** page, click on one of the endpoints for the listed project. This launches the simple .NET app.
2. In the .NET app:
  1. Visit the **Weather** and **Auth Required** pages to generate metrics. Values will be captured for `http.server.request.duration` and other instruments.
  2. On the **Home** page, click the Grafana dashboard link. This launches the ASP.NET Core dashboard in Grafana.
3. Play around inside the Grafana dashboard:
  1. Change the time range.
  2. Enable auto-refresh.
  3. Click route links to view detailed information about specific areas in the ASP.NET Core app.

For more information about using Grafana dashboards, see the [Grafana documentation](https://grafana.com/docs/grafana/latest/dashboards/use-dashboards/).
