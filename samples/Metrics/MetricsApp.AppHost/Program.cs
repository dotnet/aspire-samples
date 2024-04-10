var builder = DistributedApplication.CreateBuilder(args);

var grafana = builder.AddContainer("grafana", "grafana/grafana")
                     .WithBindMount(GetFullPath("../grafana/config"), "/etc/grafana", isReadOnly: true)
                     .WithBindMount(GetFullPath("../grafana/dashboards"), "/var/lib/grafana/dashboards", isReadOnly: true)
                     .WithHttpEndpoint(targetPort: 3000, name: "http");

builder.AddProject<Projects.MetricsApp>("app")
       .WithEnvironment("GRAFANA_URL", grafana.GetEndpoint("http"));

builder.AddContainer("prometheus", "prom/prometheus")
       .WithBindMount(GetFullPath("../prometheus"), "/etc/prometheus", isReadOnly: true)
       .WithEndpoint(/* This port is fixed as it's referenced from the Grafana config */ port: 9090, targetPort: 9090);

builder.AddExecutable("dotnet-version", "dotnet", Path.GetDirectoryName(Projects.MetricsApp_AppHost.ProjectPath)!, "--version");

using var app = builder.Build();

await app.RunAsync();

// BUG: Workaround for https://github.com/dotnet/aspire/issues/3323
static string GetFullPath(string relativePath) =>
    Path.GetFullPath(Path.Combine(Projects.MetricsApp_AppHost.ProjectPath, relativePath));
