# Aspire Samples

[![CI (main)](https://github.com/dotnet/aspire-samples/actions/workflows/ci.yml/badge.svg)](https://github.com/dotnet/aspire-samples/actions/workflows/ci.yml)

Samples for [Aspire](https://aspire.dev).

[Aspire](https://aspire.dev) is a developer-first toolset that streamlines integrating front-ends, APIs, containers, and databases with your apps. [Learn more about Aspire here](https://aspire.dev/get-started/what-is-aspire/).

## Samples in this repository

| Sample Name | Languages Used | Technologies Used | Description |
|-------------|---------------|------------------|-------------|
| [Aspire Shop](./samples/aspire-shop/) | C# | ASP.NET Core, Redis, PostgreSQL, Containers | Distributed e-commerce sample app demonstrating Aspire integration. |
| [Integrating a Node.js App](./samples/aspire-with-node) | JavaScript, C# | Node.js | Example of integrating a [Node.js](https://nodejs.org/) backend with Aspire. |
| [Integrating Frontend Apps](./samples/aspire-with-javascript) | JavaScript, TypeScript, C# | React, Vue, Angular | Demonstrates integration of popular frontend frameworks such as [React](https://react.dev/), [Vue](https://vuejs.org/), etc. |
| [Integrating Python Apps](./samples/aspire-with-python) | Python, JavaScript | FastAPI, React | Example of integrating a [FastAPI](https://fastapi.tiangolo.com/) backend and a JavaScript frontend with Aspire. |
| [Integrating a Go App](./samples/container-build) | Go | Gin, Containers | Shows how to add a [Go Gin](https://gin-gonic.com/) app being built via Dockerfile to Aspire. |
| [Integrating Orleans](./samples/orleans-voting) | C# | Orleans | Sample for distributed actor model integration with [Orleans](https://learn.microsoft.com/dotnet/orleans/overview). |
| [Persisting Data with Volume Mounts](./samples/volume-mount) | C# | Containers, Azure Storage, SQL Server | Demonstrates using volume mounts for data persistence in containers. |
| [Working with Database Containers](./samples/database-containers) | C#, SQL | PostgreSQL, MongoDB, SQL Server | Shows how to initialize and use database containers. |
| [Running EF Core Migrations](./samples/database-migrations) | C# | ASP.NET Core, Entity Framework Core | Example of running [Entity Framework Core](https://learn.microsoft.com/ef/core/) migrations in Aspire apps. |
| [Integrating Client Apps](./samples/client-apps-integration) | C# | Windows Forms, WPF | Demonstrates integration of Windows client apps using [Windows Forms](https://learn.microsoft.com/dotnet/desktop/winforms/overview/) or [WPF](https://learn.microsoft.com/dotnet/desktop/wpf/overview/). |
| [Custom Metrics Visualization](./samples/metrics) | C# | Prometheus, Grafana | Shows how to collect and visualize custom metrics using [Grafana](https://grafana.com/). |
| [Standalone Aspire dashboard](./samples/standalone-dashboard) | C# | Aspire Dashboard | Demonstrates using the standalone [Aspire dashboard](https://aspire.dev/dashboard/overview/) container to visualize OpenTelemetry from any application. |
| [Custom Aspire hosting resources](./samples/custom-resources) | C# | Aspire AppHost | Demonstrates authoring custom hosting resources with Aspire. |
| [HealthChecksUI](./samples/health-checks-ui) | C# | ASP.NET Core, Containers, Docker Compose | Demonstrates resources with separate isolated endpoints for health checks. |
| [Azure Functions](./samples/aspire-with-azure-functions) | C# | ASP.NET Core, Blazor, Azure Functions, Azure Blob Storage | Shows how to integrate [Azure Functions](https://learn.microsoft.com/azure/azure-functions/functions-overview) with Aspire. |

## eShop

[eShop](https://github.com/dotnet/eshop) is a reference application implementing an eCommerce web site on a services-based architecture using Aspire.

## Aspire Links

- [Aspire Documentation](https://aspire.dev/docs/)
- [Aspire Blog](https://devblogs.microsoft.com/aspire/)
- [Aspire GitHub](https://github.com/dotnet/aspire)

## License

These samples are licensed under the [MIT license](./LICENSE).

## Disclaimer

The sample applications provided in this repository are intended to illustrate individual concepts that may be beneficial in understanding the underlying technology and its potential uses. These samples may not illustrate best practices for production environments.

The code is not intended for operational deployment. Users should exercise caution and not rely on the samples as a foundation for any commercial or production use.

See the following links for more information on best practices and security considerations when hosting applications:

- [ASP.NET Core security topics](https://learn.microsoft.com/aspnet/core/security/)
- [Node.js security best practices](https://nodejs.org/en/learn/getting-started/security-best-practices)
- [FastAPI security](https://fastapi.tiangolo.com/tutorial/security/)

## Contributing

We welcome contributions to this repository of samples related to official Aspire features and integrations (i.e. those pieces whose code lives in the [Aspire repo](https://github.com/dotnet/aspire) and that ship from the [**Aspire** NuGet account](https://www.nuget.org/profiles/aspire)). It's generally a good idea to [log an issue](https://github.com/dotnet/aspire-samples/issues/new/choose) first to discuss any idea for a sample with the team before sending a pull request.

## Code of conduct

This project has adopted the code of conduct defined by the [Contributor Covenant](https://contributor-covenant.org) to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](https://www.dotnetfoundation.org/code-of-conduct).

## Using Devcontainer and Codespaces

This repository includes a devcontainer configuration to help you quickly set up a development environment using Visual Studio Code and GitHub Codespaces.

### Setting up Devcontainer

1. **Install Visual Studio Code**: If you haven't already, download and install [Visual Studio Code](https://code.visualstudio.com/).

2. **Install Dev Containers extension**: Open Visual Studio Code and go to the Extensions view by clicking on the Extensions icon in the Activity Bar on the side of the window. Search for "Dev Containers" and install the extension.

3. **Clone the repository**: Clone this repository to your local machine.

4. **Open the repository in Visual Studio Code**: Open Visual Studio Code and use the `File > Open Folder` menu to open the folder where you cloned this repository.

5. **Reopen in Container**: Once the repository is open, you should see a notification prompting you to reopen the folder in a container. Click the "Reopen in Container" button. If you don't see the notification, you can also use the `Remote-Containers: Reopen in Container` command from the Command Palette (Ctrl+Shift+P).

### Using GitHub Codespaces

1. **Open the repository on GitHub**: Navigate to this repository on GitHub.

2. **Create a Codespace**: Click the "Code" button and then click the "Open with Codespaces" tab. Click the "New codespace" button to create a new Codespace.

3. **Wait for the Codespace to start**: GitHub will set up a new Codespace with the devcontainer configuration defined in this repository. This may take a few minutes.

4. **Start coding**: Once the Codespace is ready, you can start coding directly in your browser or open the Codespace in Visual Studio Code.

The devcontainer configuration includes all the necessary tools and dependencies to run the samples in this repository. You can start coding and running the samples without having to install anything else on your local machine.
