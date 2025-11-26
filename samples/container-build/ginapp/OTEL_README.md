# OpenTelemetry Integration for Gin App

This application has been instrumented with OpenTelemetry to emit traces, metrics, and logs.

## Features

### 🔍 Traces
- Automatic instrumentation of all Gin HTTP endpoints via `otelgin` middleware
- Distributed tracing with trace context propagation
- HTTP request/response details automatically captured

### 📊 Metrics
- HTTP request metrics (duration, count, status codes)
- Automatic runtime metrics from the Go process

### 📝 Logs
- Structured logging using Go's standard `log/slog` package
- Logs are automatically bridged to OpenTelemetry via `otelslog`
- Correlation with traces using context

## Dependencies

The following OpenTelemetry packages are used:

- `go.opentelemetry.io/contrib/instrumentation/github.com/gin-gonic/gin/otelgin` - Gin middleware for automatic HTTP instrumentation
- `go.opentelemetry.io/contrib/bridges/otelslog` - Bridge for Go's slog to OpenTelemetry logs
- `go.opentelemetry.io/otel` - Core OpenTelemetry API
- `go.opentelemetry.io/otel/sdk` - OpenTelemetry SDK for traces, metrics, and logs
- OTLP HTTP exporters for traces, metrics, and logs

## Configuration via Environment Variables

The application uses the OpenTelemetry environment variable specification for configuration. No code changes are needed to configure endpoints or protocols.

**Important:** OpenTelemetry is **only initialized if `OTEL_EXPORTER_OTLP_ENDPOINT` is set**. If this variable is not configured, the app runs without any OpenTelemetry overhead, using standard console logging instead. This ensures the app works perfectly fine in environments where OpenTelemetry is not available or desired.

### Required Environment Variables

```bash
PORT=8080                              # Application port (required by app)
```

### OpenTelemetry Environment Variables

All standard OTEL environment variables are supported:

#### Exporter Configuration

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318    # Default OTLP endpoint
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf            # Protocol (http/protobuf or grpc)
OTEL_EXPORTER_OTLP_HEADERS=key1=value1,key2=value2   # Optional headers
OTEL_EXPORTER_OTLP_INSECURE=true                     # Skip TLS verification (for self-signed certs)

# Signal-specific endpoints (override the general endpoint)
OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=http://localhost:4318/v1/traces
OTEL_EXPORTER_OTLP_METRICS_ENDPOINT=http://localhost:4318/v1/metrics
OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=http://localhost:4318/v1/logs
```

**Important for Aspire:** When running in containers with HTTPS endpoints like `https://host.docker.internal:21227`, the Go OTLP exporters will try to verify TLS certificates. Since Aspire uses self-signed certificates, you must set `OTEL_EXPORTER_OTLP_INSECURE=true` or the connection will fail silently.

#### Service Configuration
```bash
OTEL_SERVICE_NAME=ginapp                # Service name (defaults to "ginapp")
OTEL_RESOURCE_ATTRIBUTES=key=value      # Additional resource attributes
```

#### Trace Configuration
```bash
OTEL_TRACES_SAMPLER=parentbased_always_on    # Sampling strategy
OTEL_PROPAGATORS=tracecontext,baggage        # Context propagation format
```

### Application-Specific Variables
```bash
TRUSTED_PROXIES=192.168.1.0/24;10.0.0.0/8   # Semicolon-separated proxy networks
TRUSTED_PROXIES=all                          # Trust all proxies
```

## Usage

### Running Without OpenTelemetry

The app works perfectly fine without OpenTelemetry configured. Simply run with the PORT variable:

```bash
export PORT=8080
go run ginapp.go
```

The app will use standard console logging and run without any telemetry overhead.

### Running Locally with OpenTelemetry Collector

1. Run the app with OpenTelemetry configured:
```bash
export PORT=8080
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318
export OTEL_SERVICE_NAME=ginapp
go run ginapp.go
```

2. The app will automatically:
   - Send traces to `http://localhost:4318/v1/traces`
   - Send metrics to `http://localhost:4318/v1/metrics`
   - Send logs to `http://localhost:4318/v1/logs`

### Running with Aspire

Aspire automatically configures OpenTelemetry environment variables when you add the app to the AppHost. The following are set automatically:

- `OTEL_EXPORTER_OTLP_ENDPOINT` - Points to Aspire's OTLP endpoint
- `OTEL_SERVICE_NAME` - Set to the resource name
- `OTEL_RESOURCE_ATTRIBUTES` - Includes service instance ID and other metadata

No additional configuration is needed!

## Logging

The application uses Go's standard `log/slog` package for structured logging. All logs are automatically:

1. Written to stdout (for console visibility)
2. Sent to OpenTelemetry via the `otelslog` bridge
3. Correlated with traces when using `slog.InfoContext(ctx, ...)`

### Using Logging in Your Code

```go
import "log/slog"

// Simple logging
slog.Info("Something happened", "key", "value")

// Context-aware logging (correlates with traces)
slog.InfoContext(c.Request.Context(), "Processing request", "user", userID)

// Different log levels
slog.Debug("Debug information")
slog.Info("Informational message")
slog.Warn("Warning message")
slog.Error("Error occurred", "error", err)
```

### Alternative Logging Libraries

While this implementation uses `log/slog` (Go's standard logging library), other popular options with good OpenTelemetry support include:

- **Logrus** with `otellogrus` - Widely used, mature library
- **Zap** with `otelzap` - High-performance structured logging
- **Zerolog** - Fast, zero-allocation logger (community bridges available)

The advantage of `log/slog` is that it's part of the standard library (Go 1.21+) and has official OpenTelemetry bridge support.

## Implementation Details

### Automatic Instrumentation

The `otelgin.Middleware()` is added to the Gin router, which automatically:
- Creates a span for each HTTP request
- Captures HTTP method, path, status code, and other metadata
- Propagates trace context from incoming requests
- Records request duration and other metrics

### Graceful Shutdown

The application implements graceful shutdown to ensure:
- OpenTelemetry providers are properly flushed
- Pending telemetry data is exported before termination
- Clean shutdown on SIGINT/SIGTERM signals

## Testing

Make requests to verify instrumentation:

```bash
curl http://localhost:8080/
curl http://localhost:8080/health
```

Check your OpenTelemetry backend (Jaeger, Zipkin, Aspire Dashboard, etc.) to see:
- HTTP request traces with timing information
- Structured logs with trace correlation
- HTTP metrics (request count, duration, status codes)

## Troubleshooting

### App won't start?

Make sure the `PORT` environment variable is set.

### No telemetry appearing?

1. Verify `OTEL_EXPORTER_OTLP_ENDPOINT` is set correctly
2. Check that the OTLP collector is running and accessible
3. Look for OpenTelemetry initialization errors in application logs
4. Ensure network connectivity to the collector endpoint

### Logs not appearing in OpenTelemetry?

1. Verify `OTEL_EXPORTER_OTLP_ENDPOINT` is set (required for OTEL to initialize)
2. Use `slog` for logging (not `fmt.Println`)
3. Use `slog.InfoContext(c.Request.Context(), ...)` to correlate with traces
4. Check the log exporter configuration

### Want to disable telemetry?

Either:
- Don't set `OTEL_EXPORTER_OTLP_ENDPOINT` (simplest - no OTEL overhead)
- Or set `OTEL_SDK_DISABLED=true`
