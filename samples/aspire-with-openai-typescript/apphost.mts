import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

// Add the OpenAI resource and a chat model.
const openai = await builder.addOpenAI("openai");
const chat = await openai.addModel("chat", "gpt-4o-mini");

// Run the Python FastAPI app and expose its HTTP endpoint externally.
const app = await builder
    .addUvicornApp("app", "./app", "main:app")
    .withUv()
    .withReference(chat)
    .withExternalHttpEndpoints()
    .withHttpHealthCheck({ path: "/health" });

// Run the Vite frontend after the API and inject the API URL for local proxying.
const frontend = await builder
    .addViteApp("frontend", "./frontend")
    .withUrl("", { displayText: "Frontend" })
    .withReference(app)
    .waitFor(app);

// Bundle the frontend build output into the API container for publish/deploy.
await app.publishWithContainerFiles(frontend, "./static");

await builder.build().run();
