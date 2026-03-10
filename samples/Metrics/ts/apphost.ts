// Setup: No additional packages required (uses core container and project APIs).
//
// AddOpenTelemetryCollector is a custom AppHost extension method defined in the C# project
// (MetricsApp.AppHost.OpenTelemetryCollector) and requires [AspireExport] to be available here.

import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

const prometheus = await builder.addContainer("prometheus", "prom/prometheus", "v3.2.1")
    .withBindMount("../prometheus", "/etc/prometheus", true)
    .withArgs("--web.enable-otlp-receiver", "--config.file=/etc/prometheus/prometheus.yml")
    .withHttpEndpoint({ targetPort: 9090 });
// POLYGLOT GAP: .WithUrlForEndpoint("http", u => u.DisplayText = "Prometheus Dashboard") — lambda URL customization is not available.

const prometheusHttpEndpoint = prometheus.getEndpoint("http");

const grafana = await builder.addContainer("grafana", "grafana/grafana")
    .withBindMount("../grafana/config", "/etc/grafana", true)
    .withBindMount("../grafana/dashboards", "/var/lib/grafana/dashboards", true)
    .withEnvironment("PROMETHEUS_ENDPOINT", prometheusHttpEndpoint)
    .withHttpEndpoint({ targetPort: 3000 });
// POLYGLOT GAP: .WithUrlForEndpoint("http", u => u.DisplayText = "Grafana Dashboard") — lambda URL customization is not available.

// POLYGLOT GAP: AddOpenTelemetryCollector("otelcollector", ...) is a custom C# extension
// method from MetricsApp.AppHost.OpenTelemetryCollector. To use it here, it would need
// [AspireExport] annotation and to be distributed as a NuGet package.

const app = builder.addProject("app")
    .withEnvironment("GRAFANA_URL", grafana.getEndpoint("http"));
// POLYGLOT GAP: .WithUrlForEndpoint callbacks for display text/location are not available.

await builder.build().run();
