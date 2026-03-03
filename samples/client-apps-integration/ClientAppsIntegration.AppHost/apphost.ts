import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

// POLYGLOT GAP: AddProject<Projects.ClientAppsIntegration_ApiService>("apiservice") — generic type parameter for project reference is not available.
const apiService = builder.addProject("apiservice");

// POLYGLOT GAP: OperatingSystem.IsWindows() — platform check is not available in the TypeScript apphost context.
// The following Windows-only projects cannot be conditionally added:
// if (OperatingSystem.IsWindows()) {
//   builder.AddProject<Projects.ClientAppsIntegration_WinForms>("winformsclient")
//     .WithReference(apiService).WaitFor(apiService).WithExplicitStart().ExcludeFromManifest();
//   builder.AddProject<Projects.ClientAppsIntegration_WPF>("wpfclient")
//     .WithReference(apiService).WaitFor(apiService).WithExplicitStart().ExcludeFromManifest();
// }
// POLYGLOT GAP: .WithExplicitStart() — explicit start configuration is not available.
// POLYGLOT GAP: .ExcludeFromManifest() — manifest exclusion is not available.

await builder.build().run();
