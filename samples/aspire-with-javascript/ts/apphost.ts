import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const weatherApi = await builder.addProject("weatherapi", "../AspireJavaScript.MinimalApi/AspireJavaScript.MinimalApi.csproj", "https")
    .withExternalHttpEndpoints();

await builder.addJavaScriptApp("angular", "../AspireJavaScript.Angular", { runScriptName: "start" })
    .withServiceReference(weatherApi)
    .waitFor(weatherApi)
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints()
    .publishAsDockerFile();

await builder.addJavaScriptApp("react", "../AspireJavaScript.React", { runScriptName: "start" })
    .withServiceReference(weatherApi)
    .waitFor(weatherApi)
    .withEnvironment("BROWSER", "none")
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints()
    .publishAsDockerFile();

await builder.addJavaScriptApp("vue", "../AspireJavaScript.Vue")
    .withRunScript("start")
    .withNpm({ installCommand: "ci" })
    .withServiceReference(weatherApi)
    .waitFor(weatherApi)
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints()
    .publishAsDockerFile();

const reactVite = await builder.addViteApp("reactvite", "../AspireJavaScript.Vite")
    .withServiceReference(weatherApi)
    .withEnvironment("BROWSER", "none");

await weatherApi.publishWithContainerFiles(reactVite, "./wwwroot");

await builder.build().run();
