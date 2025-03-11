var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddNpmApp("api", "../api", "run")
                 .WithHttpEndpoint(env: "PORT");

builder.AddNpmApp("app", "../app")
       .WithReference(api)
       .WaitFor(api)
       .WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
       .WithHttpEndpoint(env: "PORT")
       .WithExternalHttpEndpoints()
       .PublishAsDockerFile();

builder.Build().Run();
