import { NodeSDK } from '@opentelemetry/sdk-node';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';

const sdk = process.env.OTEL_EXPORTER_OTLP_ENDPOINT ? new NodeSDK({
    traceExporter: new OTLPTraceExporter(),
    instrumentations: [getNodeAutoInstrumentations({
        '@opentelemetry/instrumentation-fs': { enabled: false },
        '@opentelemetry/instrumentation-http': {
            ignoreIncomingRequestHook: request => request.url === '/health',
        },
    })],
}) : undefined;
sdk?.start();

export async function shutdownTelemetry() {
    await sdk?.shutdown();
}
