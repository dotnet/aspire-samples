import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const prometheus = await builder.addContainer("prometheus", "prom/prometheus:v3.2.1")
    .withBindMount("../prometheus", "/etc/prometheus", { isReadOnly: true })
    .withArgs(["--web.enable-otlp-receiver", "--config.file=/etc/prometheus/prometheus.yml"])
    .withHttpEndpoint({ targetPort: 9090 });

const grafana = await builder.addContainer("grafana", "grafana/grafana")
    .withBindMount("../grafana/config", "/etc/grafana", { isReadOnly: true })
    .withBindMount("../grafana/dashboards", "/var/lib/grafana/dashboards", { isReadOnly: true })
    .withHttpEndpoint({ targetPort: 3000 });

await builder.addProject("app", "../MetricsApp/MetricsApp.csproj", "https");

await builder.build().run();
