import { EndpointProperty, createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();
const mavenWrapper = process.platform === "win32" ? "tools/run-mvnw.cmd" : "./mvnw";

// Disposable demo database: no persistent volume; the API lets Hibernate update its schema.
const postgres = await builder.addPostgres("postgres");
const database = await postgres.addDatabase("petclinic");

// Local demo: the HTTP API has no authentication or transport encryption. Use synthetic owner data only.
const api = await builder.addJavaApp("api", "./api")
  .withWrapperPath(mavenWrapper)
  .withMavenGoal("spring-boot:run", [])
  .withOtelAgentDefaultPath()
  .withJvmArgs(["-Xms128m", "-Xmx512m"])
  .withHttpEndpoint({ targetPort: 8080, env: "SERVER_PORT" })
  .withEnvironment("SPRING_DATASOURCE_URL", await database.jdbcConnectionString())
  .withEnvironment("SPRING_DATASOURCE_USERNAME", await postgres.userNameReference())
  .withEnvironment("SPRING_DATASOURCE_PASSWORD", postgres.passwordParameter.get())
  .withReference(database)
  .waitFor(database)
  .withHttpHealthCheck({ path: "/actuator/health" })
  .withExternalHttpEndpoints()
  .withUrls(async (context) => {
    const endpoint = context.getEndpoint("http");
    const urls = await context.urls();
    await urls.addForEndpoint(endpoint, `${await endpoint.url()}/scalar`, { displayText: "API Reference" });
  });

// Angular listens on all interfaces; keep both HTTP endpoints private and do not publicly forward them.
await builder.addJavaScriptApp("frontend", "./frontend", { runScriptName: "start" })
  .withHttpEndpoint({ targetPort: 4200 })
  .withEnvironment("PETCLINIC_API_URL", api.getEndpoint("http").property(EndpointProperty.Url))
  .withReference(api)
  .waitFor(api)
  .withHttpHealthCheck({ path: "/" })
  .withExternalHttpEndpoints();

await builder.build().run();
