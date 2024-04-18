var builder = DistributedApplication.CreateBuilder(args);

var weatherApi =
    builder.AddProject<Projects.AspireJavaScript_MinimalApi>("weatherapi");

// Angular: npm run start
builder.AddNpmApp("angular", "../AspireJavaScript.Angular")
    .WithReference(weatherApi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// React: npm run start
builder.AddNpmApp("react", "../AspireJavaScript.React")
    .WithEnvironment("REACT_APP_WEATHER_API_HTTP", weatherApi.GetEndpoint("http"))
    .WithEnvironment("REACT_APP_WEATHER_API_HTTPS", weatherApi.GetEndpoint("https"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// Vue: npm run dev
builder.AddNpmApp("vue", "../AspireJavaScript.Vue", "dev")
    .WithEnvironment("VITE_WEATHER_API_HTTP", weatherApi.GetEndpoint("http"))
    .WithEnvironment("VITE_WEATHER_API_HTTPS", weatherApi.GetEndpoint("https"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
