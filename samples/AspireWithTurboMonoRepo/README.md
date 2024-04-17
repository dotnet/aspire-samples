---
languages:
- csharp
- javascript
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire with Turbo Mono Repo and NextJS example"
urlFragment: "aspire-turbo-nextjs"
description: "An example of how to integrate the official Vercel turbo nextjs kitchen sink example into a .NET Aspire app."
---

# Example using a turbo mono repo with .NET Aspire

This sample demonstrates a .NET Aspire application hosting several Node apps organized in a turbo monorepo format. 

The sample is based on [the official Vercel Turbo Kitchen Sink](https://vercel.com/templates/remix/turborepo-kitchensink)

- **AspireTurboMonoRepo.Api**: This is an [Express](https://expressjs.com/) server.
- **AspireTurboMonoRepo.Storefront**: This is a [Next.js](https://nextjs.org/) app.
- **AspireTurboMonoRepo.Admin**: This is a [Vite](https://vitejs.dev/) single page app
- **AspireTurboMonoRepo.Blog**: This is a [Remix](https://remix.run/) blog.

The JavaScript and TypeScript souce code for these apps is located in the `kitchen-sink` directory.

## Pre-requisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js](https://nodejs.org) - at least version 20.7.0
- [pnpm](https://pnpm.io) - via `npm install -g pnpm`
- **Optional** [Visual Studio 2022 17.9 Preview](https://visualstudio.microsoft.com/vs/preview/)

## Running the app

If using Visual Studio, open the solution file `AspireTurboMonoRepo.sln` and launch/debug the `AspireTurboMonoRepo.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `AspireTurboMonoRepo.AppHost` directory.

## Experiencing the app

Once the app is running, the .NET Aspire dashboard will launch in your browser 
(it may take a few seconds before the AppHost project is ready to serve the dashboard).

From the dashboard, you can navigate to the Api, Storefront, Admin, and Blog apps.

NOTE: The kitchen-sink/apps/api app does not return a value
for the default ("/") endpoint path. To test that the api app is
working, visit the "/status" endpoint, which should return `{ "ok": true }`

## Updating the kitchen-sink sample version

If you are updating this example to use a newer version of [the official Vercel Turbo Kitchen Sink](https://vercel.com/templates/remix/turborepo-kitchensink) 
source code, there a couple of small edits you will need to make to the Vercel sample code to make it 
compatible with Aspire, as described in [README_KITCHEN-SINK_UPDATES.md](README_KITCHEN-SINK_UPDATES.md)