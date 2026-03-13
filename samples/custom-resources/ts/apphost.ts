import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

// Custom resources are defined in C# extensions and are not available in the TypeScript SDK.
// This sample demonstrates the C# custom resource extensibility model.
// See the cs/ directory for the full implementation.

await builder.build().run();
