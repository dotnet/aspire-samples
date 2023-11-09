const process = require('node:process');
const http = require('node:http');
const express = require('express');
const { createTerminus, HealthCheckError } = require('@godaddy/terminus')

const app = express();
const port = process.env.PORT ?? 8080;

const cacheAddress = process.env['ConnectionStrings__cache'];
const apiServer = process.env['services__weatherapi__1'];
const otlpServer = process.env.OTEL_EXPORTER_OTLP_ENDPOINT;

app.get('/', (req, res) => {
    res.send('ok');
});

const server = http.createServer(app)

async function healthCheck () {
    const errors = [];
    return Promise.all([
        async () => {
            const apiServerHealthAddress = `${apiAddress}/health`;
            console.log(`Fetching ${apiServerHealthAddress}`);
            var response = await fetch(apiServerHealthAddress);
            if (!response.ok) {
                throw new Error(`Fetching ${apiServerHealthAddress} failed with HTTP status: ${response.status}`);
            }
        }
    ].map(p => p.catch((error) => {
        // silently collecting all the errors
        errors.push(error)
        return undefined;
    }))).then(() => {
        if (errors.length) {
            throw new HealthCheckError('healthcheck failed', errors);
        }
    });
}

createTerminus(server, {
    signal: 'SIGINT',
    healthChecks: {
        '/health': healthCheck,
        '/alive': () => { }
    },
    onSignal: () => console.log('server is starting cleanup'),
    onShutdown: () => console.log('cleanup finished, server is shutting down')
});

server.listen(port, () => { 
    console.log(`Listening on port ${port}`);
});