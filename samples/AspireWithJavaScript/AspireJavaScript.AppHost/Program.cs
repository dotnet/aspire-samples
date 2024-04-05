var builder = DistributedApplication.CreateBuilder(args);

var weatherApi =
    builder.AddProject<Projects.AspireJavaScript_MinimalApi>("weatherapi");

// Angular: npm run start
builder.AddNpmApp("angular", "../AspireJavaScript.Angular")
    .WithReference(weatherApi)
    .WithHttpEndpoint(targetPort: 3000, env: "PORT")
    .PublishAsDockerFile();

// React: npm run start
builder.AddNpmApp("react", "../AspireJavaScript.React")
    .WithReference(weatherApi)
    .WithHttpEndpoint(targetPort: 3001, env: "PORT")
    .PublishAsDockerFile();

// Vue: npm run dev
builder.AddNpmApp("vue", "../AspireJavaScript.Vue", "dev")
    .WithReference(weatherApi)
    .WithHttpEndpoint(targetPort: 3002, env: "PORT")
    .PublishAsDockerFile();

builder.Build().Run();
