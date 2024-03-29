﻿var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var weatherapi = builder.AddProject<Projects.AspireWithNode_AspNetCoreApi>("weatherapi");

builder.AddNpmApp("frontend", "../NodeFrontend", "watch")
    .WithReference(weatherapi)
    .WithReference(cache)
    .WithEndpoint(containerPort: 3000, scheme: "http", env: "PORT")
    .PublishAsDockerFile();

builder.Build().Run();
