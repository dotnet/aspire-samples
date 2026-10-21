var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject<Projects.AspireJavaScript_MinimalApi>("weatherapi")
    .WithExternalHttpEndpoints();

builder.AddJavaScriptApp("angular", "../AspireJavaScript.Angular", runScriptName: "start")
    .WithNpm(installCommand: "ci")
    .WithReference(weatherApi)
    .WaitFor(weatherApi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile(container => container.WithEntrypoint("/docker-entrypoint.sh"));

builder.AddJavaScriptApp("react", "../AspireJavaScript.React", runScriptName: "start")
    .WithNpm(installCommand: "ci")
    .WithReference(weatherApi)
    .WaitFor(weatherApi)
    .WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile(container => container.WithEntrypoint("/docker-entrypoint.sh"));

builder.AddJavaScriptApp("vue", "../AspireJavaScript.Vue")
    .WithRunScript("start")
    .WithNpm(installCommand: "ci")
    .WithReference(weatherApi)
    .WaitFor(weatherApi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile(container => container.WithEntrypoint("/docker-entrypoint.sh"));

var reactVite = builder.AddViteApp("reactvite", "../AspireJavaScript.Vite")
    .WithNpm(installCommand: "ci")
    .WithReference(weatherApi)
    .WithEnvironment("BROWSER", "none");

// Bundle the output of the Vite app into the wwwroot of the weather API
weatherApi.PublishWithContainerFiles(reactVite, "./wwwroot");

builder.Build().Run();
