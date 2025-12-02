# Integrating Angular, React, and Vue with Aspire

This sample demonstrates using the Aspire JavaScript hosting integration to configure and run client-side applications.

The app consists of four services:

- **AspireJavaScript.MinimalApi**: This is an HTTP API that returns randomly generated weather forecast data.
- **AspireJavaScript.Angular**: This is an Angular app that consumes the weather forecast API and displays the data in a table.
- **AspireJavaScript.React**: This is a React app that consumes the weather forecast API and displays the data in a table.
- **AspireJavaScript.Vue**: This is a Vue app that consumes the weather forecast API and displays the data in a table.

## Pre-requisites

- [Aspire development environment](https://aspire.dev/get-started/prerequisites/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org) - at least version 24.x

## Running the app

If using the Aspire CLI, run `aspire run` from this directory.

If using VS Code, open this directory as a workspace and launch the `AspireShop.AppHost` project using either the Aspire or C# debuggers.

If using Visual Studio, open the solution file `AspireShop.slnx` and launch/debug the `AspireShop.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `AspireShop.AppHost` directory.

### Experiencing the app

Once the app is running, the Aspire dashboard will launch in your browser:

![Aspire dashboard](./images/aspire-dashboard.png)

From the dashboard, you can navigate to the Angular, React, and Vue apps:

**Angular**

![Angular app](./images/angular-app.png)

**React**

![React app](./images/react-app.png)

**Vue**

![Vue app](./images/vue-app.png)
