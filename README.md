# Aspire Samples

[![CI (main)](https://github.com/dotnet/aspire-samples/actions/workflows/ci.yml/badge.svg)](https://github.com/dotnet/aspire-samples/actions/workflows/ci.yml)

Samples for [Aspire](https://aspire.dev).

[Aspire](https://aspire.dev) is a developer-first toolset that streamlines integrating front-ends, APIs, containers, and databases with your apps. [Learn more about Aspire here](https://aspire.dev/get-started/what-is-aspire/).

## Samples in this repository

| Sample Name | Languages Used | Technologies Used | Description |
|-------------|---------------|------------------|-------------|
| [Aspire Shop](./samples/AspireShop/) | C# | ASP.NET Core, Redis, PostgreSQL, Containers | Distributed e-commerce sample app demonstrating Aspire integration. |
| [Integrating a Node.js App](./samples/AspireWithNode) | JavaScript, C# | Node.js | Example of integrating a Node.js backend with Aspire. |
| [Integrating Frontend Apps](./samples/AspireWithJavaScript) | JavaScript, TypeScript, C# | React, Vue, Angular, Aspire | Demonstrates integration of popular frontend frameworks. |
| [Integrating Python Apps](./samples/AspireWithPython) | Python, JavaScript | FastAPI, React | Example of integrating a Python backend and a JavaScript frontend with Aspire. |
| [Integrating a Go App](./samples/ContainerBuild) | Go | Gin, Containers | Shows how to add a Go app being built via Dockerfile to Aspire. |
| [Integrating Orleans](./samples/OrleansVoting) | C# | Orleans | Sample for distributed actor model integration with Orleans. |
| [Persisting Data with Volume Mounts](./samples/VolumeMount) | C# | Containers, Azure Storage, SQL Server | Demonstrates using volume mounts for data persistence in containers. |
| [Working with Database Containers](./samples/DatabaseContainers) | C#, SQL | PostgreSQL, MongoDB, SQL Server | Shows how to initialize and use database containers. |
| [Running EF Core Migrations](./samples/DatabaseMigrations) | C# | ASP.NET Core, Entity Framework Core | Example of running EF Core migrations in Aspire apps. |
| [Integrating Client Apps](./samples/ClientAppsIntegration) | C# | Windows Forms, WPF | Demonstrates integration of Windows client apps using Windows Forms or WPF. |
| [Custom Metrics Visualization](./samples/Metrics) | C# | Prometheus, Grafana | Shows how to collect and visualize custom metrics using Grafana. |
| [Standalone Aspire dashboard](./samples/StandaloneDashboard) | C# | Aspire Dashboard | Demonstrates using the standalone Aspire dashboard container to visualize OpenTelemetry from any application. |
| [Custom Aspire hosting resources](./samples/CustomResources) | C# | Aspire AppHost | Demonstrates authoring custom hosting resources with Aspire. |
| [HealthChecksUI](./samples/HealthChecksUI) | C# | ASP.NET Core, Containers, Docker Compose | Demonstrates resources with separate isolated endpoints for health checks. |
| [Azure Functions](./samples/AspireWithAzureFunctions) | C# | ASP.NET Core, Blazor, Azure Functions, Azure Blob Storage | Shows how to integrate Azure Functions with Aspire. |

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
