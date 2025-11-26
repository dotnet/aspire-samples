package main

import (
	"strconv"
	"time"

	"github.com/gin-gonic/gin"
	"go.opentelemetry.io/contrib/instrumentation/github.com/gin-gonic/gin/otelgin"
	"go.opentelemetry.io/otel/attribute"
	otelmetric "go.opentelemetry.io/otel/metric"
)

// setupMiddleware configures all middleware for the Gin router
func setupMiddleware(router *gin.Engine, otelEnabled bool) {
	if !otelEnabled {
		return
	}

	// Add OpenTelemetry tracing middleware
	router.Use(otelgin.Middleware("ginapp"))

	// Add custom metrics middleware
	router.Use(metricsMiddleware())
}

// metricsMiddleware records HTTP request metrics
func metricsMiddleware() gin.HandlerFunc {
	return func(c *gin.Context) {
		start := time.Now()

		// Process request
		c.Next()

		// Record metrics
		duration := float64(time.Since(start).Milliseconds())
		statusCode := strconv.Itoa(c.Writer.Status())

		attrs := otelmetric.WithAttributes(
			attribute.String("http.method", c.Request.Method),
			attribute.String("http.route", c.FullPath()),
			attribute.String("http.status_code", statusCode),
		)

		if httpRequestCounter != nil {
			httpRequestCounter.Add(c.Request.Context(), 1, attrs)
		}

		if httpDuration != nil {
			httpDuration.Record(c.Request.Context(), duration, attrs)
		}
	}
}
