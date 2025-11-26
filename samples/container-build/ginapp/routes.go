package main

import (
	"log/slog"

	"github.com/gin-gonic/gin"
)

// setupRoutes configures all HTTP routes for the application
func setupRoutes(router *gin.Engine) {
	// Define a route that listens to GET requests on /
	router.GET("/", func(c *gin.Context) {
		slog.InfoContext(c.Request.Context(), "Handling root request")
		c.JSON(200, gin.H{
			"message": "Hot reload is working FOR REALZ! 🎉🔥",
		})
	})

	// Define a health check endpoint
	router.GET("/health", func(c *gin.Context) {
		slog.InfoContext(c.Request.Context(), "Health check requested")
		c.JSON(200, gin.H{
			"status": "healthy",
		})
	})
}
