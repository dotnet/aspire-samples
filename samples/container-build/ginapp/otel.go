package main

import (
	"context"
	"crypto/tls"
	"fmt"
	"os"

	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/exporters/otlp/otlplog/otlploggrpc"
	"go.opentelemetry.io/otel/exporters/otlp/otlpmetric/otlpmetricgrpc"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracegrpc"
	"go.opentelemetry.io/otel/log/global"
	otelmetric "go.opentelemetry.io/otel/metric"
	"go.opentelemetry.io/otel/propagation"
	"go.opentelemetry.io/otel/sdk/log"
	"go.opentelemetry.io/otel/sdk/metric"
	"go.opentelemetry.io/otel/sdk/resource"
	"go.opentelemetry.io/otel/sdk/trace"
	semconv "go.opentelemetry.io/otel/semconv/v1.26.0"
	"google.golang.org/grpc/credentials"
)

var (
	httpRequestCounter otelmetric.Int64Counter
	httpDuration       otelmetric.Float64Histogram
)

// isInsecureEndpoint checks if TLS verification should be skipped
func isInsecureEndpoint() bool {
	// Check the OTEL_EXPORTER_OTLP_INSECURE environment variable
	if insecure := os.Getenv("OTEL_EXPORTER_OTLP_INSECURE"); insecure == "true" {
		return true
	}
	return false
}

func initOpenTelemetry(ctx context.Context) (shutdown func(context.Context) error, enabled bool, err error) {
	// Check if OpenTelemetry is configured
	// If OTEL_EXPORTER_OTLP_ENDPOINT or OTEL_SDK_DISABLED is not set, skip initialization
	otlpEndpoint := os.Getenv("OTEL_EXPORTER_OTLP_ENDPOINT")
	sdkDisabled := os.Getenv("OTEL_SDK_DISABLED")

	if sdkDisabled == "true" || otlpEndpoint == "" {
		// Return a no-op shutdown function
		return func(context.Context) error { return nil }, false, nil
	}

	// Log that we're initializing OpenTelemetry
	fmt.Printf("Initializing OpenTelemetry with endpoint: %s\n", otlpEndpoint)
	fmt.Printf("OTEL_EXPORTER_OTLP_PROTOCOL: %s\n", os.Getenv("OTEL_EXPORTER_OTLP_PROTOCOL"))
	fmt.Printf("OTEL_EXPORTER_OTLP_INSECURE: %s\n", os.Getenv("OTEL_EXPORTER_OTLP_INSECURE"))
	fmt.Printf("OTEL_SERVICE_NAME: %s\n", os.Getenv("OTEL_SERVICE_NAME"))

	var shutdownFuncs []func(context.Context) error

	// Create shutdown function that calls all cleanup functions
	shutdown = func(ctx context.Context) error {
		var err error
		for _, fn := range shutdownFuncs {
			err = fn(ctx)
		}
		return err
	}

	// Handle errors by calling shutdown for cleanup
	handleErr := func(inErr error) {
		err = inErr
		shutdown(ctx)
	}

	// Create resource with service name from environment variable or default
	serviceName := os.Getenv("OTEL_SERVICE_NAME")
	if serviceName == "" {
		serviceName = "ginapp"
	}

	res, err := resource.Merge(
		resource.Default(),
		resource.NewWithAttributes(
			semconv.SchemaURL,
			semconv.ServiceName(serviceName),
		),
	)
	if err != nil {
		handleErr(err)
		return
	}

	// Set up propagator (supports OTEL_PROPAGATORS env var)
	otel.SetTextMapPropagator(propagation.NewCompositeTextMapPropagator(
		propagation.TraceContext{},
		propagation.Baggage{},
	))

	// Set up trace provider
	// Note: The exporters automatically read OTEL_EXPORTER_OTLP_* environment variables
	var traceExporterOpts []otlptracegrpc.Option
	if isInsecureEndpoint() {
		// Use TLS but skip certificate verification (for self-signed certs)
		tlsConfig := &tls.Config{
			InsecureSkipVerify: true,
		}
		traceExporterOpts = append(traceExporterOpts, otlptracegrpc.WithTLSCredentials(credentials.NewTLS(tlsConfig)))
		fmt.Println("Trace exporter configured with TLS certificate verification disabled")
	}
	traceExporter, err := otlptracegrpc.New(ctx, traceExporterOpts...)
	if err != nil {
		fmt.Printf("Failed to create trace exporter: %v\n", err)
		handleErr(err)
		return
	}
	tracerProvider := trace.NewTracerProvider(
		trace.WithBatcher(traceExporter),
		trace.WithResource(res),
	)
	shutdownFuncs = append(shutdownFuncs, tracerProvider.Shutdown)
	otel.SetTracerProvider(tracerProvider)

	// Set up metric provider
	var metricExporterOpts []otlpmetricgrpc.Option
	if isInsecureEndpoint() {
		tlsConfig := &tls.Config{
			InsecureSkipVerify: true,
		}
		metricExporterOpts = append(metricExporterOpts, otlpmetricgrpc.WithTLSCredentials(credentials.NewTLS(tlsConfig)))
	}
	metricExporter, err := otlpmetricgrpc.New(ctx, metricExporterOpts...)
	if err != nil {
		fmt.Printf("Failed to create metric exporter: %v\n", err)
		handleErr(err)
		return
	}
	meterProvider := metric.NewMeterProvider(
		metric.WithReader(metric.NewPeriodicReader(metricExporter)),
		metric.WithResource(res),
	)
	shutdownFuncs = append(shutdownFuncs, meterProvider.Shutdown)
	otel.SetMeterProvider(meterProvider)

	// Set up log provider
	var logExporterOpts []otlploggrpc.Option
	if isInsecureEndpoint() {
		tlsConfig := &tls.Config{
			InsecureSkipVerify: true,
		}
		logExporterOpts = append(logExporterOpts, otlploggrpc.WithTLSCredentials(credentials.NewTLS(tlsConfig)))
	}
	logExporter, err := otlploggrpc.New(ctx, logExporterOpts...)
	if err != nil {
		fmt.Printf("Failed to create log exporter: %v\n", err)
		handleErr(err)
		return
	}
	loggerProvider := log.NewLoggerProvider(
		log.WithProcessor(log.NewBatchProcessor(logExporter)),
		log.WithResource(res),
	)
	shutdownFuncs = append(shutdownFuncs, loggerProvider.Shutdown)
	global.SetLoggerProvider(loggerProvider)

	fmt.Println("OpenTelemetry initialized successfully")

	// Initialize metrics
	meter := otel.Meter("ginapp")
	httpRequestCounter, err = meter.Int64Counter(
		"http.server.request.count",
		otelmetric.WithDescription("Number of HTTP requests received"),
		otelmetric.WithUnit("{request}"),
	)
	if err != nil {
		fmt.Printf("Failed to create request counter: %v\n", err)
	}

	httpDuration, err = meter.Float64Histogram(
		"http.server.request.duration",
		otelmetric.WithDescription("Duration of HTTP requests"),
		otelmetric.WithUnit("ms"),
	)
	if err != nil {
		fmt.Printf("Failed to create duration histogram: %v\n", err)
	}

	return shutdown, true, nil
}
