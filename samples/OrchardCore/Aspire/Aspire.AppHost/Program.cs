var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedisContainer("Redis", 50963);

builder.AddProject<Projects.OrchardCore_Cms>("OrchardCore CMS")
    .WithReference(redis);

builder.AddProject<Projects.OrchardCore_Mvc>("OrchardCore MVC");

builder.Build().Run();
