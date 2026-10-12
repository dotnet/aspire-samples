#:package Aspire.Hosting.Redis@13.6.0
#:sdk Aspire.AppHost.Sdk@13.6.0
#:property AspireUseCliBundle=true

var builder = DistributedApplication.CreateBuilder(args);

// The optional HTTP profile in apphost.run.json keeps dashboard authentication but not transport encryption.
// Keep its dashboard, telemetry, and resource-service loopback endpoints private; do not forward them.

#pragma warning disable ASPIRETERMINAL001
// No persistent volume: use disposable data. The authenticated Redis REPL is for trusted dashboard users only.
var redis = builder.AddRedis("redis").WithRepl();

// Local demo: anyone who can reach this unauthenticated HTTP API can read, replace, or delete demo notes.
var api = builder.AddProject("api", "./api/Notes.Api.csproj")
    .WithReference(redis)
    .WithHttpHealthCheck("/health")
    .WaitFor(redis);

builder.AddDockerfile("slumber", "./slumber")
    .WithEnvironment("BASE_URL", api.GetEndpoint("http"))
    .WaitFor(api)
    .WithTerminal();
#pragma warning restore ASPIRETERMINAL001

builder.Build().Run();
