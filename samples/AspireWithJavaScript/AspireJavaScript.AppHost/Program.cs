var builder = DistributedApplication.CreateBuilder(args);

var weatherApi =
    builder.AddProject<Projects.AspireJavaScript_MinimalApi>("weatherapi");

// Angular: npm run start
builder.AddNpmApp("angular", "../AspireJavaScript.Angular")
    .WithReference(weatherApi)
    .WithEndpoint(containerPort: 3000, scheme: "http", env: "PORT")
    .AsDockerfileInManifest();

// React: npm run start
builder.AddNpmApp("react", "../AspireJavaScript.React")
    .WithReference(weatherApi)
    .WithEndpoint(containerPort: 3001, scheme: "http", env: "PORT")
    .AsDockerfileInManifest();

// Vue: npm run dev
builder.AddNpmApp("vue", "../AspireJavaScript.Vue", "dev")
    .WithReference(weatherApi)
    .WithEndpoint(containerPort: 3002, scheme: "http", env: "PORT")
    .AsDockerfileInManifest();

builder.Build().Run();
