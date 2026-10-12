import { createClient } from 'redis';
import { createApp } from './app.ts';
import { shutdownTelemetry } from './instrumentation.ts';

const url = process.env.REDIS_URI;
const port = Number(process.env.PORT);
if (!url || !Number.isInteger(port) || port < 1 || port > 65535) {
    throw new Error('Aspire must supply REDIS_URI and a valid PORT.');
}

const redis = createClient({
    url,
    disableOfflineQueue: true,
    socket: { connectTimeout: 5000 },
});
redis.on('error', error => console.error('Redis connection error:', error));
await redis.connect();
const server = createApp(redis).listen(port, '0.0.0.0', () => {
    console.log(`Notes API listening on port ${port}`);
});

let stopping = false;
async function stop() {
    if (stopping) return;
    stopping = true;
    try {
        try {
            await new Promise<void>((resolve, reject) => {
                server.close(error => error ? reject(error) : resolve());
            });
        } finally {
            redis.destroy();
            await shutdownTelemetry();
        }
    } catch (error) {
        console.error('Shutdown failed:', error);
        process.exitCode = 1;
    }
}
process.on('SIGTERM', () => { void stop(); });
process.on('SIGINT', () => { void stop(); });
