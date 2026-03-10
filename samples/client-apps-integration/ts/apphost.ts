import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const apiService = builder.addProject("apiservice", "../ClientAppsIntegration.ApiService/ClientAppsIntegration.ApiService.csproj", "https");

await builder.build().run();
