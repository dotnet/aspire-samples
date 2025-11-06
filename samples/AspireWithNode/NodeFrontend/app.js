// Import instrumentation first - MUST be before any other imports
import { createLogger } from './instrumentation.js';

import http from 'node:http';
import https from 'node:https';
import fs from 'node:fs';
import express from 'express';
import { createClient } from 'redis';

// Read configuration from environment variables
const config = {
    environment: process.env.NODE_ENV || 'development',
    httpPort: process.env['PORT'] ?? 8080,
    httpsPort: process.env['HTTPS_PORT'] ?? 8443,
    httpsRedirectPort: process.env['HTTPS_REDIRECT_PORT'] ?? (process.env['HTTPS_PORT'] ?? 8443),
    httpsRedirectHost: process.env.HOST ?? 'localhost',
    certFile: process.env['HTTPS_CERT_FILE'] ?? '',
    certKeyFile: process.env['HTTPS_CERT_KEY_FILE'] ?? '',
    cacheUri: process.env['CACHE_URI'] ?? '',
    apiServer: process.env['services__weatherapi__https__0'] ?? process.env['services__weatherapi__http__0']
};

// Setup HTTPS options
const httpsOptions = fs.existsSync(config.certFile) && fs.existsSync(config.certKeyFile)
    ? {
        cert: fs.readFileSync(config.certFile),
        key: fs.readFileSync(config.certKeyFile),
        enabled: true
    }
    : { enabled: false };

const startupLogger = createLogger('nodefrontend.startup');
startupLogger.info('Application starting', { httpsEnabled: httpsOptions.enabled });

// Setup connection to Redis cache
let cacheConfig = {
    url: config.cacheUri
};
const cache = config.cacheUri ? createClient(cacheConfig) : null;
if (cache) {
    cache.on('error', err => startupLogger.error('Redis Client Error', { error: err }));
    await cache.connect();
    startupLogger.info('Connected to Redis cache');
}

// Setup express app
const app = express();

// Middleware to redirect HTTP to HTTPS
function httpsRedirect(req, res, next) {
    if (req.secure || req.headers['x-forwarded-proto'] === 'https') {
        // Request is already HTTPS
        return next();
    }
    // Redirect to HTTPS
    const redirectTo = new URL(`https://${config.httpsRedirectHost}:${config.httpsRedirectPort}${req.url}`);
    const logger = createLogger('nodefrontend.httpsRedirect');
    logger.info('Redirecting to HTTPS', { url: redirectTo.toString() });
    res.redirect(redirectTo);
}
if (httpsOptions.enabled) {
    app.use(httpsRedirect);
}

app.get('/', async (req, res) => {
    const logger = createLogger('nodefrontend.getForecastsEndpoint');
    if (cache) {
        const cachedForecasts = await cache.get('forecasts');
        if (cachedForecasts) {
            logger.info('Cache hit for forecasts');
            res.render('index', { forecasts: JSON.parse(cachedForecasts) });
            return;
        }
    }

    logger.info('Cache miss - fetching from API', { apiServer: config.apiServer });
    const response = await fetch(`${config.apiServer}/weatherforecast`);
    const forecasts = await response.json();
    if (cache) {
        await cache.set('forecasts', JSON.stringify(forecasts), { 'EX': 30 }); // Cache for 30 seconds
        logger.info('Forecasts cached for 30 seconds');
    }
    res.render('index', { forecasts: forecasts });
});

// Configure templating
app.set('views', './views');
app.set('view engine', 'pug');

// Health check endpoint
app.get('/health', async (req, res) => {
    const logger = createLogger('nodefrontend.healthEndpoint');
    try {
        const apiServerHealthAddress = `${config.apiServer}/health`;
        logger.info('Health check - fetching API health', { url: apiServerHealthAddress });
        
        const response = await fetch(apiServerHealthAddress);
        if (!response.ok) {
            logger.error('API health check failed', { 
                url: apiServerHealthAddress, 
                status: response.status 
            });
            return res.status(503).send('Unhealthy');
        }
        
        logger.info('Health check passed');
        res.status(200).send('Healthy');
    } catch (error) {
        logger.error('API health check error', { 
            url: `${config.apiServer}/health`, 
            error: error.message 
        });
        res.status(503).send('Unhealthy');
    }
});

// Liveness endpoint
app.get('/alive', (req, res) => {
    const logger = createLogger('nodefrontend.aliveEndpoint');
    logger.info('Liveness check');
    res.status(200).send('Healthy');
});

// Start servers
const httpServer = http.createServer(app);
const httpsServer = httpsOptions.enabled ? https.createServer(httpsOptions, app) : null;

httpServer.listen(config.httpPort, () => {
    startupLogger.info('HTTP server started', {
        type: 'HTTP',
        port: config.httpPort,
        address: httpServer.address()
    });
});

if (httpsServer) {
    httpsServer.listen(config.httpsPort, () => {
        startupLogger.info('HTTPS server started', {
            type: 'HTTPS',
            port: config.httpsPort,
            address: httpsServer.address()
        });
    });
}

// Register signal handlers
process.on('SIGINT', () => gracefulShutdown('SIGINT'));
process.on('SIGTERM', () => gracefulShutdown('SIGTERM'));

// Graceful shutdown handler
let isShuttingDown = false;
let cleanupDone = false;

async function gracefulShutdown(signal) {
    if (isShuttingDown) return;
    isShuttingDown = true;

    const logger = createLogger('nodefrontend.shutdown');
    logger.info(`Received ${signal}, starting graceful shutdown`);

    // Close servers
    logger.info('Closing servers...');
    const closePromises = [];
    closePromises.push(closeServer(httpServer));
    closePromises.push(closeServer(httpsServer));
    await Promise.all(closePromises);
    logger.info('All servers closed');

    // Cleanup resources
    if (!cleanupDone) {
        cleanupDone = true;
        if (cache) {
            logger.info('Closing Redis connection');
            try {
                await cache.disconnect();
                logger.info('Redis connection closed');
            } catch (error) {
                logger.error('Error closing Redis connection', { error: error.message });
            }
        }
    }

    logger.info('Graceful shutdown complete');
    process.exit(0);

    function closeServer(httpServer) {
        if (!httpServer) return Promise.resolve();
        const serverType = httpServer instanceof https.Server ? 'HTTPS' : 'HTTP'
        logger.info(`Closing ${serverType} server...`);
        return new Promise(resolve => {
            httpServer.close(() => {
                logger.info(`${serverType} server closed`);
                resolve();
            });
            httpServer.closeAllConnections();
        });
    }
}