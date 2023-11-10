import { env } from 'node:process';
import { NodeSDK } from '@opentelemetry/sdk-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-grpc';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-grpc';
import { SimpleLogRecordProcessor } from '@opentelemetry/sdk-logs';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { HttpInstrumentation } from '@opentelemetry/instrumentation-http';
import { ExpressInstrumentation } from '@opentelemetry/instrumentation-express';
import { RedisInstrumentation } from '@opentelemetry/instrumentation-redis-4';

const otlpServer = env.OTEL_EXPORTER_OTLP_ENDPOINT;

if (otlpServer) {
    console.log(`OTLP endpoint: ${otlpServer}`);

    const sdk = new NodeSDK({
        traceExporter: new OTLPTraceExporter({
            url: `${otlpServer}/v1/traces`,
            headers: {},
        }),
        metricReader: new PeriodicExportingMetricReader({
            exporter: new OTLPMetricExporter({
                url: `${otlpServer}/v1/metrics`
            }),
        }),
        logRecordProcessor: new SimpleLogRecordProcessor({
            exporter: new OTLPLogExporter({
                url: `${otlpServer}/v1/logs`
            })
        }),
        instrumentations: [
            new HttpInstrumentation(),
            new ExpressInstrumentation(),
            new RedisInstrumentation()
        ],
    });

    sdk.start();
} 