var builder = DistributedApplication.CreateBuilder(args);

var weatherApi =
    builder.AddProject<Projects.AspireJavaScript_MinimalApi>("weatherapi");

// Angular: npm run start
builder.AddNpmApp("angular", "../AspireJavaScript.Angular")
    .WithReference(weatherApi)
    .WithHttpEndpoint(containerPort: 4000, env: "PORT")
    .PublishAsDockerFile();

// React: npm run start
builder.AddNpmApp("react", "../AspireJavaScript.React")
    .WithReference(weatherApi)
    .WithHttpEndpoint(containerPort: 4001, env: "PORT")
    .PublishAsDockerFile();

// Vue: npm run dev
builder.AddNpmApp("vue", "../AspireJavaScript.Vue", "dev")
    .WithReference(weatherApi)
    .WithHttpEndpoint(containerPort: 4002, env: "PORT")
    .PublishAsDockerFile();

builder.Build().Run();
