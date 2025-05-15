---
languages:
- csharp
- go
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire Custom Resources Sample"
urlFragment: "aspire-custom-resources"
description: "An example of writing custom resources for .NET Aspire hosting integrations."
---

# Writing custom resources for .NET Aspire hosting integrations

This sample demonstrates how to write custom resources for .NET Aspire hosting integrations. This is useful when you want to integrate something into the Aspire development experience as a resource that isn't an executable or container. Custom resources can particpate in the Aspire development experience, including the dashboard, and can be used to integrate with other tools or services.

Custom resources are defined using C# and generally consist of a class that implements the `IResource` interface and some extension methods to enable adding them to an `IDistributedApplicationBuilder`. Custom resources can publish and respond to events to give them "life" and allow them to interact with the rest of the Aspire application.

In this sample, we define a `TalkingClock` custom resource that spawns child `ClockHand` resources that tick on and off every second. We also define a `TestResource` custom resource that simply cycles through a set of states.

Read more about the [Aspire resource model here](https://gist.github.com/davidfowl/b408af870d4b5b54a28bf18bffa127e1).

## Pre-requisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Optional** [Visual Studio 2022 17.12+](https://visualstudio.microsoft.com/vs/)

## Running the app

If using Visual Studio, open the solution file `CustomResources.sln` and launch/debug the `CustomResources.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `CustomResources.AppHost` directory.

If using the Aspire CLI, run `aspire run` from the `CustomResources` directory.
