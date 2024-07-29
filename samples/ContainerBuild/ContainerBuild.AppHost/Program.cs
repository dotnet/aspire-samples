var builder = DistributedApplication.CreateBuilder(args);

var goVersion = builder.AddParameter("goversion"); // Value set in appsettings.json and overrides the default
                                                   // specified in the Dockerfile.

builder.AddDockerfile("ginapp", "../ginapp")
       .WithEndpoint(scheme: "http", targetPort: 5555, env: "PORT")
       .WithBuildArg("GO_VERSION", goVersion);

builder.Build().Run();
