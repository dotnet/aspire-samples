# .NET Aspire Samples

[![CI (main)](https://github.com/dotnet/aspire-samples/actions/workflows/ci.yml/badge.svg)](https://github.com/dotnet/aspire-samples/actions/workflows/ci.yml)

Samples for .NET Aspire.

[.NET Aspire](https://aka.ms/aspireannouncement) is a stack for building resilient, observable, cloud-native apps with .NET.

## Official Samples

Official samples hosted in this repo can be accessed via the [Samples browser](https://learn.microsoft.com/samples/browse/?expanded=dotnet&products=dotnet-aspire).

Sample highlights include:

- [Aspire Shop](./samples/AspireShop/)
- [Custom metrics visualization with OpenTelemetry, Prometheus & Grafana](./samples/Metrics)
- [Integrating a Node.js app](./samples/AspireWithNode)
- [Integrating frontend apps using React, Vue, Angular, etc.](./samples/AspireWithJavaScript)
- [Integrating a Go app using a Dockerfile](./samples/ContainerBuild)
- [Integrating Orleans](./samples/OrleansVoting)
- [Persisting data in composed containers using volume mounts](./samples/VolumeMount)
- [Working with and initializing database containers](./samples/DatabaseContainers)
- [Running Entity Framework Core migrations](./samples/DatabaseMigrations)
- [Integrating clients apps like WinForms](./samples/ClientAppsIntegration)

## eShop

[eShop](https://github.com/dotnet/eshop) is a reference .NET application implementing an eCommerce web site using a services-based architecture using .NET Aspire.

## .NET Aspire Links

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [.NET Aspire Blog](https://aka.ms/dotnet/aspire/blog)
- [.NET Aspire GitHub](https://github.com/dotnet/aspire)

## License

.NET (including the aspire-samples repo) is licensed under the [MIT license](./LICENSE).

## Disclaimer

The sample applications provided in this repository are intended to illustrate individual concepts that may be beneficial in understanding the underlying technology and its potential uses. These samples may not illustrate best practices for production environments.

The code is not intended for operational deployment. Users should exercise caution and not rely on the samples as a foundation for any commercial or production use. See [ASP.NET Core security topics](https://learn.microsoft.com/aspnet/core/security/) for more information on security concerns related to hosting ASP.NET Core applications.

## Contributing

We welcome contributions to this repository of samples related to official .NET Aspire features and integrations (i.e. those pieces whose code lives in the [Aspire repo](https://github.com/dotnet/aspire) and that ship from the [**Aspire** NuGet account](https://www.nuget.org/profiles/aspire)). It's generally a good idea to [log an issue](https://github.com/dotnet/aspire-samples/issues/new/choose) first to discuss any idea for a sample with the team before sending a pull request.

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
