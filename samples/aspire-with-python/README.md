# Integrating a FastAPI (Python) app within an Aspire application

This sample demonstrates integrating a FastAPI (Python) app and a JavaSript frontend using Aspire.

The sample consists of two apps:

- **app**: This is a simple FastAPI-based Python app that returns randomly generated weather forecast data.
- **frontend**: This is a Vite-based React app that renders the weather forecast data.

## Prerequisites

- [Aspire development environment](https://aspire.dev/get-started/prerequisites/)
- [Python](https://www.python.org/) - at least version 3.13
- [Node.js](https://nodejs.org) - at least version 22.21.1
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Running the app

If using the Aspire CLI, run `aspire run` from this directory.

If using VS Code, open this directory as a workspace and launch the `apphost.cs` C# file using either the Aspire or C# debuggers.
