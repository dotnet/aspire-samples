using Listly.AppHost;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal;

var builder = DistributedApplication.CreateBuilder(args);

var listlydb = builder.AddPostgresContainer("listly", 1433, "1q2w3e4r5T*")
    .AddDatabase("listlydb");

var api = builder.AddProject<Projects.Listly_Service_API>("listly.service.api")
    .WithReference(listlydb);

builder.AddProject<Projects.Listly_Database>("lisltlydbapp")
    .WithReference(listlydb);

builder.AddNpmApp("frontend", "../listly-web", "dev")
    .WithReference(api)
    .WithServiceBinding(scheme: "http");

builder.Build().Run();