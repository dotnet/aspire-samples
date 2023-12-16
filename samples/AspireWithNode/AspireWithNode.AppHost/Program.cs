var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var weatherapi = builder.AddProject<Projects.AspireWithNode_AspNetCoreApi>("weatherapi");

builder.AddNpmApp("frontend", "../NodeFrontend", "watch")
    .WithReference(weatherapi)
    .WithReference(cache)
    // This is a workaround for https://github.com/dotnet/aspire/issues/1430
    // .WithServiceBinding(scheme: "http", env: "PORT")
    .WithAnnotation(new ServiceBindingAnnotation(System.Net.Sockets.ProtocolType.Tcp, uriScheme: "http", containerPort: 3000, env: "PORT"))
    .AsDockerfileInManifest();

builder.Build().Run();
