var builder = DistributedApplication.CreateBuilder(args);

var cmsdb = builder.AddPostgresContainer("Postgres", 62262, "OrchardCorePass")
    .WithEnvironment("POSTGRES_USER", "occms")
    .WithEnvironment("POSTGRES_PASSWORD", "OrchardCorePass");
    
var redis = builder.AddRedisContainer("Redis", 50963);

builder.AddProject<Projects.OrchardCore_Cms>("OrchardCore CMS")
    .WithReference(redis)
    .WithReference(cmsdb);

await builder.Build().RunAsync();
