var builder = DistributedApplication.CreateBuilder(args);

var grafana = builder.AddContainer("grafana", "grafana/grafana")
                     .WithBindMount("../grafana/config", "/etc/grafana", isReadOnly: true)
                     .WithBindMount("../grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
                     .WithEndpoint(targetPort: 3000, port: 3000, name: "grafana-http", scheme: "http");

builder.AddProject<Projects.MetricsApp>("app")
       .WithEnvironment("GRAFANA_URL", grafana.GetEndpoint("grafana-http"));

builder.AddContainer("prometheus", "prom/prometheus")
       .WithBindMount("../prometheus", "/etc/prometheus", isReadOnly: true)
       .WithEndpoint(targetPort: 9090, port: 9090);

using var app = builder.Build();

await app.RunAsync();
