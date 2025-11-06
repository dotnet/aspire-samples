import { env } from 'node:process';
import { NodeSDK } from '@opentelemetry/sdk-node';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-grpc';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-grpc';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { HttpInstrumentation } from '@opentelemetry/instrumentation-http';
import { ExpressInstrumentation } from '@opentelemetry/instrumentation-express';
import { UndiciInstrumentation } from '@opentelemetry/instrumentation-undici';
import { RedisInstrumentation } from '@opentelemetry/instrumentation-redis-4';
import winston from 'winston';
import { OpenTelemetryTransportV3 } from '@opentelemetry/winston-transport';

const environment = process.env.NODE_ENV || 'development';

// For OpenTelemetry troubleshooting, uncomment the following lines and set the log level to DiagLogLevel.DEBUG
//import { diag, DiagConsoleLogger, DiagLogLevel } from '@opentelemetry/api';
//diag.setLogger(new DiagConsoleLogger(), environment === 'development' ? DiagLogLevel.DEBUG : DiagLogLevel.WARN);

const otlpServer = env.OTEL_EXPORTER_OTLP_ENDPOINT;

if (otlpServer) {
    console.log(`OTLP endpoint: ${otlpServer}`);

    const sdk = new NodeSDK({
        traceExporter: new OTLPTraceExporter(),
        metricReader: new PeriodicExportingMetricReader({
            exportIntervalMillis: environment === 'development' ? 5000 : 10000,
            exporter: new OTLPMetricExporter(),
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

// Setup Winston logger factory with OpenTelemetry transport
export function createLogger(category = 'nodefrontend') {
    return winston.createLogger({
        level: 'info', // This is the min level, anything lower won't be sent
        format: winston.format.json(),
        defaultMeta: { 
            category: category
        },
        transports: [
            new winston.transports.Console({
                format: winston.format.combine(
                    winston.format.colorize(),
                    winston.format.simple()
                )
            }),
            otlpServer ? new OpenTelemetryTransportV3() : null
        ].filter(Boolean)
    });
}