# Aspire Samples

[![CI (main)](https://github.com/dotnet/aspire-samples/actions/workflows/ci.yml/badge.svg)](https://github.com/dotnet/aspire-samples/actions/workflows/ci.yml)

Samples for [Aspire](https://aspire.dev).

[Aspire](https://aspire.dev) is a developer-first toolset that streamlines integrating front-ends, APIs, containers, and databases with your apps. [Learn more about Aspire here](https://aspire.dev/get-started/what-is-aspire/).

## Aspire version

These samples use the released Aspire **13.6.0** CLI and SDK.
The repository's [`nuget.config`](./nuget.config) restores packages from NuGet.org.
The Java hosting integration is published as a preview package alongside this release.

Install the matching Aspire CLI before building or running the samples:

```bash
curl -fsSL https://aspire.dev/install.sh | bash -s -- --version 13.6.0
```

On Windows, use PowerShell:

```powershell
& ([scriptblock]::Create((Invoke-RestMethod https://aspire.dev/install.ps1))) -Version 13.6.0
```

Dev Containers and CI install the same CLI version automatically.

## Browse by scenario

- [Full-stack JavaScript / TypeScript](#full-stack-javascript--typescript)
- [Polyglot full-stack](#polyglot-full-stack)
- [Backend integrations by language](#backend-integrations-by-language)
- [Interactive terminals](#interactive-terminals)
- [Cloud / AI / event-driven](#cloud--ai--event-driven)
- [.NET + frontend and platform](#net--frontend-and-platform)

### Full-stack JavaScript / TypeScript

| Sample | Workload languages | AppHost | Deploy | Description |
| --- | --- | --- | --- | --- |
| [Integrating Frontend Apps](./samples/aspire-with-javascript) | JavaScript, TypeScript | C# AppHost | Run only | React, Vue, and Angular frontends integrated with Aspire. |
| [Node + Redis + Vite](./samples/node-express-redis) | JavaScript, TypeScript | TypeScript AppHost | Docker Compose | Express API + React/Vite frontend + Redis behind YARP. |
| [Node.js Weather Explorer](./samples/aspire-with-node) | JavaScript, TypeScript | TypeScript AppHost | Run only | Interactive Leaflet weather map; Express + OpenTelemetry API over the Open-Meteo external service. |
| [Vite + YARP Static Files](./samples/vite-yarp-static) | JavaScript, TypeScript | TypeScript AppHost | Docker Compose | Vite frontend served through YARP in run and publish modes. |

### Polyglot full-stack

| Sample | Workload languages | AppHost | Deploy | Description |
| --- | --- | --- | --- | --- |
| [Integrating Python Apps](./samples/aspire-with-python) | Python, JavaScript | C# AppHost | Run only | FastAPI backend + React frontend integrated with Aspire. |
| [Vite + C# + PostgreSQL](./samples/vite-csharp-postgres) | C#, JavaScript, TypeScript | TypeScript AppHost | Docker Compose | React frontend + C# API + PostgreSQL in a single Aspire app. |
| [Vite + React + FastAPI](./samples/vite-react-fastapi) | Python, JavaScript, TypeScript | TypeScript AppHost | Docker Compose | React frontend + FastAPI backend behind YARP. |
| [Spring Petclinic](./samples/spring-petclinic) | Java, TypeScript | TypeScript AppHost | Run only | Angular frontend + Spring Boot API + PostgreSQL using the official Java hosting integration. |
| [Polyglot Task Queue](./samples/polyglot-task-queue) | JavaScript, Python, C# | TypeScript AppHost | Docker Compose | React + Node API + Python/C# workers coordinated through RabbitMQ. |
| [RAG Document Q&A](./samples/rag-document-qa-svelte) | Python, JavaScript | TypeScript AppHost | Run only | Svelte frontend + FastAPI + Qdrant + OpenAI. |

### Backend integrations by language

| Sample | Workload languages | AppHost | Deploy | Description |
| --- | --- | --- | --- | --- |
| [Integrating a Go App](./samples/container-build) | Go | C# AppHost | Docker Compose | Builds and runs a Go Gin app from a Dockerfile with Aspire. |
| [Custom MailDev and MailKit integrations](./samples/maildev-mailkit) | C#, TypeScript | TypeScript AppHost | Run only | Authors custom hosting and client integrations with ATS-generated TypeScript APIs. |
| [Go API](./samples/golang-api) | Go | TypeScript AppHost | Docker Compose | Go + chi API with Aspire-managed run and publish flows. |
| [Python FastAPI + PostgreSQL](./samples/python-fastapi-postgres) | Python | TypeScript AppHost | Docker Compose | FastAPI CRUD API wired to PostgreSQL and pgAdmin. |
| [Python OpenAI Agent](./samples/python-openai-agent) | Python | TypeScript AppHost | Docker Compose | FastAPI AI agent sample with OpenAI integration. |
| [Python Script](./samples/python-script) | Python | TypeScript AppHost | Run only | Minimal Python script sample using a file-based AppHost. |

### Interactive terminals

| Sample | Workload languages | AppHost | Deploy | Description |
| --- | --- | --- | --- | --- |
| [Terminal basics: C#](./samples/terminals/basics-csharp) | C# | C# AppHost | Run only | Slumber REST TUI + notes API + Redis REPL using Aspire 13.6. |
| [Terminal basics: TypeScript](./samples/terminals/basics-typescript) | TypeScript | TypeScript AppHost | Run only | Slumber REST TUI + notes API + Redis REPL using Aspire 13.6. |
| [Terminal automation](./samples/terminals/automation-csharp) | C# | C# AppHost | Run only | Automate Slumber CRUD with terminal input, screen assertions, and API verification using Aspire 13.6. |
| [Custom docked SQL REPL](./samples/terminals/docked-repl-csharp) | C# | C# AppHost | Run only | Custom rqlite resource with an on-demand docked SQL shell using Aspire 13.6. |

### Cloud / AI / event-driven

| Sample | Workload languages | AppHost | Deploy | Description |
| --- | --- | --- | --- | --- |
| [Azure Functions](./samples/aspire-with-azure-functions) | C# | C# AppHost | Azure | Integrates Azure Functions, ASP.NET Core, and Blazor with Aspire. |
| [Image Gallery](./samples/image-gallery) | C#, JavaScript, TypeScript | C# AppHost | Azure | Upload and process images with Azure Blob, Queues, SQL, and Container Apps Jobs. |
| [Custom Metrics Visualization](./samples/Metrics) | C# | C# AppHost | Run only | Collects and visualizes custom metrics with Prometheus and Grafana. |
| [Standalone Aspire dashboard](./samples/standalone-dashboard) | C# | C# AppHost | Run only | Runs the Aspire dashboard container against any OpenTelemetry source. |

### .NET + frontend and platform

| Sample | Workload languages | AppHost | Deploy | Description |
| --- | --- | --- | --- | --- |
| [Aspire Shop](./samples/aspire-shop) | C# | C# AppHost | Run only | Distributed e-commerce sample app demonstrating Aspire integration. |
| [HealthChecksUI](./samples/health-checks-ui) | C# | C# AppHost | Docker Compose | Demonstrates isolated health endpoints with HealthChecksUI. |
| [Integrating Client Apps](./samples/client-apps-integration) | C# | C# AppHost | Run only | Integrates Windows Forms and WPF apps with Aspire. |
| [Integrating Orleans](./samples/orleans-voting) | C# | C# AppHost | Run only | Distributed actor model sample built with Orleans. |
| [Working with Database Containers](./samples/database-containers) | C#, SQL | C# AppHost | Run only | Initializes and uses PostgreSQL, MongoDB, and SQL Server containers. |
| [Running EF Core Migrations](./samples/database-migrations) | C# | C# AppHost | Run only | Runs Entity Framework Core migrations inside Aspire workflows. |
| [Persisting Data with Volume Mounts](./samples/volume-mount) | C# | C# AppHost | Run only | Demonstrates data persistence with containers, Azure Storage, and SQL Server. |
| [Custom Aspire hosting resources](./samples/custom-resources) | C# | C# AppHost | Run only | Demonstrates authoring custom hosting resources with Aspire. |

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
