import env from 'node:process';
import http from 'node:http';
import https from 'node:https';
import fs from 'node:fs';
import fetch from 'node-fetch';
import express from 'express';
import { createTerminus, HealthCheckError } from '@godaddy/terminus';
import { createClient } from 'redis';

const environment = process.env.NODE_ENV || 'development';
const app = express();
const httpPort = env.PORT ?? 8080;
const httpsPort = env.HTTPS_PORT ?? 8443;
const certFile = env.HTTPS_CERT_FILE ?? '';
const certKeyFile = env.HTTPS_CERT_KEY_FILE ?? '';
const httpsOptions = fs.existsSync(certFile) && fs.existsSync(certKeyFile)
    ? {
        cert: fs.readFileSync(certFile),
        key: fs.readFileSync(certKeyFile)
    }
    : null;

const cacheAddress = env['ConnectionStrings__cache'];
const apiServer = env['services__weatherapi__https__0'] ?? env['services__weatherapi__http__0'];
const passwordPrefix = ",password=";

var cacheConfig = {
    url: `redis://${cacheAddress}`
};

let cachePasswordIndex = cacheAddress.indexOf(passwordPrefix);

if (cachePasswordIndex > 0) {
    cacheConfig = {
        url: `redis://${cacheAddress.substring(0, cachePasswordIndex)}`,
        password: cacheAddress.substring(cachePasswordIndex + passwordPrefix.length)
    }
}

console.log(`environment: ${environment}`);
console.log(`cacheAddress: ${cacheAddress}`);
console.log(`apiServer: ${apiServer}`);

const cache = createClient(cacheConfig);
cache.on('error', err => console.log('Redis Client Error', err));
await cache.connect();

app.get('/', async (req, res) => {
    let cachedForecasts = await cache.get('forecasts');
    if (cachedForecasts) {
        res.render('index', { forecasts: JSON.parse(cachedForecasts) });
        return;
    }

    let response = await fetch(`${apiServer}/weatherforecast`);
    let forecasts = await response.json();
    await cache.set('forecasts', JSON.stringify(forecasts), { 'EX': 5 });
    res.render('index', { forecasts: forecasts });
});

app.set('views', './views');
app.set('view engine', 'pug');

const httpServer = http.createServer(app)
const httpsServer = httpsOptions ? https.createServer(httpsOptions, app) : null;

async function healthCheck() {
    const errors = [];
    const apiServerHealthAddress = `${apiServer}/health`;
    console.log(`Fetching ${apiServerHealthAddress}`);
    try {
        var response = await fetch(apiServerHealthAddress);
        if (!response.ok) {
            console.log(`Failed fetching ${apiServerHealthAddress}. ${response.status}`);
            throw new HealthCheckError(`Fetching ${apiServerHealthAddress} failed with HTTP status: ${response.status}`);
        }
    } catch (error) {
        console.log(`Failed fetching ${apiServerHealthAddress}. ${error}`);
        throw new HealthCheckError(`Fetching ${apiServerHealthAddress} failed with HTTP status: ${error}`);
    }
}

function startTerminus(server) {
    createTerminus(httpServer, {
        signal: 'SIGINT',
        healthChecks: {
            '/health': healthCheck,
            '/alive': () => { }
        },
        onSignal: async () => {
            console.log('server is starting cleanup');
            console.log('closing Redis connection');
            await cache.disconnect();
        },
        onShutdown: () => console.log('cleanup finished, server is shutting down')
    });
}

startTerminus(httpServer);
if (httpsServer) {
    startTerminus(httpsServer);
}

httpServer.listen(httpPort, () => {
    console.log(`HTTP listening on port ${httpPort}`);
});
if (httpsServer) {
    httpsServer.listen(httpsPort, () => {
        console.log(`HTTPS listening on port ${httpsPort}`);
    });
}
