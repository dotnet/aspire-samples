import { createBuilder } from "./.modules/aspire.js";
import os from "os";

const builder = await createBuilder();

const apiService = await builder.addProject(
  "apiservice",
  "../ClientAppsIntegration.ApiService/ClientAppsIntegration.ApiService.csproj",
  "https",
);

if (os.platform() === "win32") {
  builder
    .addProject(
      "winformsclient",
      "../ClientAppsIntegration.WinForms/ClientAppsIntegration.WinForms.csproj",
      "ClientAppsIntegration.WinForms"
    )
    .withServiceReference(apiService)
    .waitFor(apiService)
    .withExplicitStart()
    .excludeFromManifest();

  builder
    .addProject(
      "wpfclient",
      "../ClientAppsIntegration.WPF/ClientAppsIntegration.WPF.csproj",
      "ClientAppsIntegration.WPF"
    )
    .withServiceReference(apiService)
    .waitFor(apiService)
    .withExplicitStart()
    .excludeFromManifest();
}

await builder.build().run();
