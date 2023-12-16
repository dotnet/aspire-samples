import { env } from 'node:process';
import { createServer } from 'node:http';
import fetch from 'node-fetch';
import express from 'express';
import { createTerminus, HealthCheckError } from '@godaddy/terminus';
import { createClient } from 'redis';

const app = express();
const port = env.PORT ?? 8080;

const cacheAddress = env['ConnectionStrings__cache'];
const apiServer = env['services__weatherapi__1'];
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

const server = createServer(app)

async function healthCheck() {
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
    onSignal: async () => {
        console.log('server is starting cleanup');
        console.log('closing Redis connection');
        await cache.disconnect();
    },
    onShutdown: () => console.log('cleanup finished, server is shutting down')
});

server.listen(port, () => {
    console.log(`Listening on port ${port}`);
});