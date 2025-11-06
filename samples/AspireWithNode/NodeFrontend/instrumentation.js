import { env } from 'node:process';
import { NodeSDK } from '@opentelemetry/sdk-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-grpc';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { HttpInstrumentation } from '@opentelemetry/instrumentation-http';
import { ExpressInstrumentation } from '@opentelemetry/instrumentation-express';
import { UndiciInstrumentation } from '@opentelemetry/instrumentation-undici';
import { RedisInstrumentation } from '@opentelemetry/instrumentation-redis-4';
import { diag, DiagConsoleLogger, DiagLogLevel } from '@opentelemetry/api';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';
import { credentials } from '@grpc/grpc-js';
import { name, version } from './version.js';

const environment = process.env.NODE_ENV || 'development';

// For OpenTelemetry troubleshooting, uncomment the following line and set the log level to DiagLogLevel.DEBUG
//diag.setLogger(new DiagConsoleLogger(), environment === 'development' ? DiagLogLevel.DEBUG : DiagLogLevel.WARN);

const otlpServer = env.OTEL_EXPORTER_OTLP_ENDPOINT;

if (otlpServer) {
    console.log(`OTLP endpoint: ${otlpServer}`);

    const isHttps = otlpServer.startsWith('https://');
    const collectorOptions = {
        credentials: !isHttps
            ? credentials.createInsecure()
            : credentials.createSsl()
    };

    // Create resource with service name
    const resource = resourceFromAttributes({
        [ATTR_SERVICE_NAME]: env.OTEL_SERVICE_NAME || name,
        [ATTR_SERVICE_VERSION]: version || 'unknown',
    });

    const sdk = new NodeSDK({
        resource: resource,
        traceExporter: new OTLPTraceExporter(collectorOptions),
        metricReader: new PeriodicExportingMetricReader({
            exportIntervalMillis: environment === 'development' ? 5000 : 10000,
            exporter: new OTLPMetricExporter(collectorOptions),
        }),
        instrumentations: [
            new HttpInstrumentation(),
            // BUG: The Express instrumentation doesn't currently work for some reason
            new ExpressInstrumentation(),
            new UndiciInstrumentation(),
            new RedisInstrumentation()
        ],
    });

    sdk.start();
} 