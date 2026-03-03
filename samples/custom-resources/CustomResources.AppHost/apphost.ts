import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

// POLYGLOT GAP: AddTalkingClock("talking-clock") is a custom resource extension (from CustomResources.AppHost)
// and is not available in the TypeScript polyglot SDK.
// builder.AddTalkingClock("talking-clock");

// POLYGLOT GAP: AddTestResource("test") is a custom resource extension (from CustomResources.AppHost)
// and is not available in the TypeScript polyglot SDK.
// builder.AddTestResource("test");

await builder.build().run();
