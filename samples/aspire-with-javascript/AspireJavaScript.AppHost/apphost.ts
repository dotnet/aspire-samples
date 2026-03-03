import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

// POLYGLOT GAP: AddProject<Projects.AspireJavaScript_MinimalApi>("weatherapi") — generic type parameter for project reference is not available.
const weatherApi = builder.addProject("weatherapi")
    .withExternalHttpEndpoints();

// POLYGLOT GAP: AddJavaScriptApp() is not available in the TypeScript polyglot SDK.
// The following Angular, React, and Vue apps cannot be added directly.

// POLYGLOT GAP: builder.AddJavaScriptApp("angular", "../AspireJavaScript.Angular", runScriptName: "start")
//   .WithReference(weatherApi).WaitFor(weatherApi).WithHttpEndpoint(env: "PORT")
//   .WithExternalHttpEndpoints().PublishAsDockerFile()
// AddJavaScriptApp, WithHttpEndpoint with env parameter, and PublishAsDockerFile are not available.

// POLYGLOT GAP: builder.AddJavaScriptApp("react", "../AspireJavaScript.React", runScriptName: "start")
//   .WithReference(weatherApi).WaitFor(weatherApi).WithEnvironment("BROWSER", "none")
//   .WithHttpEndpoint(env: "PORT").WithExternalHttpEndpoints().PublishAsDockerFile()
// AddJavaScriptApp, WithHttpEndpoint with env parameter, and PublishAsDockerFile are not available.

// POLYGLOT GAP: builder.AddJavaScriptApp("vue", "../AspireJavaScript.Vue").WithRunScript("start").WithNpm(installCommand: "ci")
//   .WithReference(weatherApi).WaitFor(weatherApi).WithHttpEndpoint(env: "PORT")
//   .WithExternalHttpEndpoints().PublishAsDockerFile()
// AddJavaScriptApp, WithRunScript, WithNpm, WithHttpEndpoint with env parameter, and PublishAsDockerFile are not available.

// POLYGLOT GAP: builder.AddViteApp("reactvite", "../AspireJavaScript.Vite")
//   .WithReference(weatherApi).WithEnvironment("BROWSER", "none")
// AddViteApp is not available in the TypeScript polyglot SDK.

// POLYGLOT GAP: weatherApi.PublishWithContainerFiles(reactVite, "./wwwroot")
// PublishWithContainerFiles is not available in the TypeScript polyglot SDK.

await builder.build().run();
