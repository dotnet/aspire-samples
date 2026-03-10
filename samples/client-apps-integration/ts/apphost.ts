// Setup: No additional packages required (uses core project APIs).

import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

const apiService = builder.addProject("apiservice");

// The C# version conditionally adds WinForms/WPF projects on Windows using OperatingSystem.IsWindows().
// In TypeScript, we can use process.platform for the equivalent check.
if (process.platform === "win32") {
    builder.addProject("winformsclient")
        .withReference(apiService)
        .waitFor(apiService)
        .withExplicitStart()
        .excludeFromManifest();

    builder.addProject("wpfclient")
        .withReference(apiService)
        .waitFor(apiService)
        .withExplicitStart()
        .excludeFromManifest();
}

await builder.build().run();
