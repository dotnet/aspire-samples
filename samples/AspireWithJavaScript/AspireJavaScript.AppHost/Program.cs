var builder = DistributedApplication.CreateBuilder(args);

var weatherApi =
    builder.AddProject<Projects.AspireJavaScript_MinimalApi>("weatherApi");

// Angular: npm run start
builder.AddNpmApp("angular", "../AspireJavaScript.Angular")
    .WithReference(weatherApi)
    .WithServiceBinding(scheme: "http", env: "PORT")
    .AsDockerfileInManifest();

// React: npm run start
builder.AddNpmApp("react", "../AspireJavaScript.React")
    .WithReference(weatherApi)
    .WithServiceBinding(scheme: "http", env: "PORT")
    .AsDockerfileInManifest();

// Vue: npm run dev
builder.AddNpmApp("vue", "../AspireJavaScript.Vue", "dev")
    .WithReference(weatherApi)
    .WithServiceBinding(scheme: "http", env: "PORT")
    .AsDockerfileInManifest();

builder.Build().Run();
