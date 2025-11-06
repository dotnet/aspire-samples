var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var weatherapi = builder.AddProject<Projects.AspireWithNode_AspNetCoreApi>("weatherapi")
    .WithHttpHealthCheck("/health");

builder.AddNodeApp("frontend", "../NodeFrontend", "./app.js")
    // .WithArgs(c =>
    // {
    //     c.Args.Insert(0, "--import");
    //     c.Args.Insert(1, "./instrumentation.js");
    // })
    .WithNpm()
    .WithRunScript("dev")
    .WithBuildScript("build")
    .WithHttpEndpoint(port: 5223, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(weatherapi).WaitFor(weatherapi)
    .WithReference(cache).WaitFor(cache);

builder.Build().Run();
