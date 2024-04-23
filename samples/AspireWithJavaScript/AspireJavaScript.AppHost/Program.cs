var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject<Projects.AspireJavaScript_MinimalApi>("weatherapi")
    .WithExternalHttpEndpoints();

var weatherApiHttp = weatherApi.GetEndpoint("http");
var weatherApiHttps = weatherApi.GetEndpoint("https");

// Angular: npm run start
builder.AddNpmApp("angular", "../AspireJavaScript.Angular")
    .WithReference(weatherApi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// React: npm run start
var react = builder.AddNpmApp("react", "../AspireJavaScript.React")
    .WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
    .WithEnvironment("REACT_APP_WEATHER_API_HTTP", weatherApiHttp)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

if (weatherApiHttps.Exists)
{
    react.WithEnvironment("REACT_APP_WEATHER_API_HTTPS", weatherApiHttps);
}

// Vue: npm run dev
var vue = builder.AddNpmApp("vue", "../AspireJavaScript.Vue", "dev")
    .WithEnvironment("VITE_WEATHER_API_HTTP", weatherApiHttp)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

if (weatherApiHttps.Exists)
{
    vue.WithEnvironment("VITE_WEATHER_API_HTTPS", weatherApiHttps);
}

builder.Build().Run();
