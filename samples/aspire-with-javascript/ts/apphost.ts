// Setup: Run the following commands to add required integrations:
//   aspire add javascript

import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

const weatherApi = builder.addProject("weatherapi")
    .withExternalHttpEndpoints();

const angular = builder.addJavaScriptApp("angular", "../AspireJavaScript.Angular", "start")
    .withReference(weatherApi)
    .waitFor(weatherApi)
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints();
// POLYGLOT GAP: .PublishAsDockerFile() — publish-time Dockerfile generation may not be available.

const react = builder.addJavaScriptApp("react", "../AspireJavaScript.React", "start")
    .withReference(weatherApi)
    .waitFor(weatherApi)
    .withEnvironment("BROWSER", "none")
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints();
// POLYGLOT GAP: .PublishAsDockerFile() — publish-time Dockerfile generation may not be available.

const vue = builder.addJavaScriptApp("vue", "../AspireJavaScript.Vue")
    .withRunScript("start")
    .withNpm({ installCommand: "ci" })
    .withReference(weatherApi)
    .waitFor(weatherApi)
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints();
// POLYGLOT GAP: .PublishAsDockerFile() — publish-time Dockerfile generation may not be available.

const reactVite = builder.addViteApp("reactvite", "../AspireJavaScript.Vite")
    .withReference(weatherApi)
    .withEnvironment("BROWSER", "none");

// POLYGLOT GAP: weatherApi.publishWithContainerFiles(reactVite, "./wwwroot") — bundling Vite
// output into a project's wwwroot may not be available.

await builder.build().run();
