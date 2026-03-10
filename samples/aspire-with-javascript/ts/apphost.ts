import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const weatherApi = builder.addProject("weatherapi", "../AspireJavaScript.MinimalApi/AspireJavaScript.MinimalApi.csproj", "https")
    .withExternalHttpEndpoints();

builder.addJavaScriptApp("angular", "../AspireJavaScript.Angular", { runScriptName: "start" })
    .withReference(weatherApi)
    .waitFor(weatherApi)
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints()
    .publishAsDockerFile();

builder.addJavaScriptApp("react", "../AspireJavaScript.React", { runScriptName: "start" })
    .withReference(weatherApi)
    .waitFor(weatherApi)
    .withEnvironment("BROWSER", "none")
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints()
    .publishAsDockerFile();

builder.addJavaScriptApp("vue", "../AspireJavaScript.Vue")
    .withRunScript("start")
    .withNpm({ installCommand: "ci" })
    .withReference(weatherApi)
    .waitFor(weatherApi)
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints()
    .publishAsDockerFile();

const reactVite = builder.addViteApp("reactvite", "../AspireJavaScript.Vite")
    .withReference(weatherApi)
    .withEnvironment("BROWSER", "none");

weatherApi.publishWithContainerFiles(reactVite, "./wwwroot");

await builder.build().run();
