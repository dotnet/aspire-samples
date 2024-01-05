var builder = DistributedApplication.CreateBuilder(args);
const int postgresPort = 62262;
const string postgresUsername = "occms";
const string postgresPassword = "OrchardCorePass";

var cmsdb = builder.AddPostgresContainer("Postgres", postgresPort, postgresPassword);
    
var redis = builder.AddRedisContainer("Redis", 50963);

builder.AddProject<Projects.OrchardCore_Cms>("OrchardCore CMS")
    .WithEnvironment("OrchardCore__Default__State", "Uninitialized")
    .WithEnvironment("OrchardCore__Default__TablePrefix", "Default")
    .WithEnvironment("OrchardCore__DatabaseProvider", "Postgres")
    .WithEnvironment("OrchardCore__ConnectionString", $"host=localhost;port={postgresPort};database={postgresUsername};username={postgresUsername};password={postgresPassword}")
    .WithReference(redis)
    .WithReference(cmsdb);

await builder.Build().RunAsync();
