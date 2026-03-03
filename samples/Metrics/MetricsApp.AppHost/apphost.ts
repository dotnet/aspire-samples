import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

// POLYGLOT GAP: AddOpenTelemetryCollector() is a custom AppHost extension method (from MetricsApp.AppHost.OpenTelemetryCollector)
// and is not available in the TypeScript polyglot SDK.

const prometheus = await builder.addContainer("prometheus", "prom/prometheus", "v3.2.1")
    .withBindMount("../prometheus", "/etc/prometheus", true)
    .withArgs("--web.enable-otlp-receiver", "--config.file=/etc/prometheus/prometheus.yml")
    .withHttpEndpoint({ targetPort: 9090 });
// POLYGLOT GAP: .WithUrlForEndpoint("http", u => u.DisplayText = "Prometheus Dashboard") — lambda URL customization is not available in the TypeScript SDK.

const prometheusHttpEndpoint = prometheus.getEndpoint("http");

const grafana = await builder.addContainer("grafana", "grafana/grafana")
    .withBindMount("../grafana/config", "/etc/grafana", true)
    .withBindMount("../grafana/dashboards", "/var/lib/grafana/dashboards", true)
    .withEnvironment("PROMETHEUS_ENDPOINT", prometheusHttpEndpoint)
    .withHttpEndpoint({ targetPort: 3000 });
// POLYGLOT GAP: .WithUrlForEndpoint("http", u => u.DisplayText = "Grafana Dashboard") — lambda URL customization is not available in the TypeScript SDK.

// POLYGLOT GAP: builder.AddOpenTelemetryCollector("otelcollector", "../otelcollector/config.yaml")
//   .WithEnvironment("PROMETHEUS_ENDPOINT", `${prometheus.GetEndpoint("http")}/api/v1/otlp`)
// AddOpenTelemetryCollector is a custom extension method not available in the TypeScript SDK.

const app = builder.addProject("app")
    .withEnvironment("GRAFANA_URL", grafana.getEndpoint("http"));
// POLYGLOT GAP: AddProject<Projects.MetricsApp>("app") — generic type parameter for project reference is not available; use addProject("name") instead.
// POLYGLOT GAP: .WithUrlForEndpoint("https", u => u.DisplayText = "Instrumented App") — lambda URL customization is not available.
// POLYGLOT GAP: .WithUrlForEndpoint("http", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly) — lambda URL customization is not available.

await builder.build().run();
