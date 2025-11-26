package main

import (
	"context"
	"fmt"
	"log/slog"
	"os"
	"os/signal"
	"strconv"
	"strings"
	"syscall"
	"time"

	"github.com/gin-gonic/gin"
	"go.opentelemetry.io/contrib/bridges/otelslog"
)

func main() {
    ctx := context.Background()

    // Initialize OpenTelemetry (only if configured)
    shutdown, otelEnabled, err := initOpenTelemetry(ctx)
    if err != nil {
        fmt.Printf("Warning: Failed to initialize OpenTelemetry: %v\n", err)
        fmt.Println("Continuing without OpenTelemetry instrumentation...")
        // Don't exit - continue running without OTEL
        otelEnabled = false
        shutdown = func(context.Context) error { return nil }
    }
    defer func() {
        if err := shutdown(ctx); err != nil {
            fmt.Printf("Failed to shutdown OpenTelemetry: %v\n", err)
        }
    }()

    // Set up structured logging
    var logger *slog.Logger
    if otelEnabled {
        // Use OpenTelemetry bridge for logging when OTEL is enabled
        logger = slog.New(otelslog.NewHandler("ginapp"))
        slog.SetDefault(logger)
        slog.Info("Starting Gin application with OpenTelemetry enabled")
    } else {
        // Use standard text handler when OTEL is not configured
        logger = slog.New(slog.NewTextHandler(os.Stdout, nil))
        slog.SetDefault(logger)
        slog.Info("Starting Gin application (OpenTelemetry not configured)")
    }

    // Create a Gin router with default middleware: logger and recovery (crash-free) middleware
    router := gin.Default()

    // Add OpenTelemetry middleware
    setupMiddleware(router, otelEnabled)

    // Configure trusted proxies
    trustedProxies := os.Getenv("TRUSTED_PROXIES")
    if trustedProxies == "all" {
        // Trust all networks (default, no method call required)
        slog.Info("Trusting all proxy networks")
    } else if trustedProxies != "" {
        // Trust specific networks
        proxies := strings.Split(trustedProxies, ";")
        router.SetTrustedProxies(proxies)
        slog.Info("Configured trusted proxies", "proxies", proxies)
    } else {
        // Disable trusted proxies
        router.SetTrustedProxies(nil)
        slog.Info("Disabled trusted proxies")
    }

    // Configure routes
    setupRoutes(router)

    portVar := os.Getenv("PORT")
    if portVar == "" {
        slog.Error("Environment variable PORT is not set")
        return
    }

    port, err := strconv.Atoi(portVar)
    if err != nil {
        slog.Error("Error converting PORT to integer", "error", err)
        return
    }

    endpoint := fmt.Sprintf(":%d", port)
    slog.Info("Starting server", "endpoint", endpoint)

    // Set up graceful shutdown
    srv := make(chan error, 1)
    go func() {
        srv <- router.Run(endpoint)
    }()

    // Wait for interrupt signal or server error
    quit := make(chan os.Signal, 1)
    signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)

    select {
    case err := <-srv:
        if err != nil {
            slog.Error("Server failed", "error", err)
        }
    case sig := <-quit:
        slog.Info("Shutting down server", "signal", sig)
    }

    // Give outstanding requests time to complete
    time.Sleep(2 * time.Second)
    slog.Info("Server stopped")
}
