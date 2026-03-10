// Setup: No standard packages — this sample uses custom C# resource extensions.
//
// POLYGLOT GAP: AddTalkingClock and AddTestResource are custom C# resource extensions
// defined in CustomResources.AppHost. To use them from TypeScript, they would need
// [AspireExport] attributes and distribution as a NuGet package, then added via:
//   aspire add <custom-package-name>

import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

// builder.addTalkingClock("talking-clock");
// builder.addTestResource("test");

await builder.build().run();
