package main

import (
    "github.com/gin-gonic/gin"
    "fmt"
    "os"
    "strconv"
    "strings"
)

func main() {
    // Create a Gin router with default middleware: logger and recovery (crash-free) middleware
    router := gin.Default()

    // Configure trusted proxies
    trustedProxies := os.Getenv("TRUSTED_PROXIES")
    if trustedProxies != "" {
        proxies := strings.Split(trustedProxies, ";")
        router.SetTrustedProxies(proxies)
    } else {
        router.SetTrustedProxies(nil)
    }

    // Define a route that listens to GET requests on /helloworld
    router.GET("/", func(c *gin.Context) {
        c.JSON(200, gin.H{
            "message": "Hello, World!",
        })
    })

    portVar := os.Getenv("PORT")
    if portVar == "" {
        fmt.Println("Environment variable PORT is not set.")
        return
    }

    port, err := strconv.Atoi(portVar)
    if err != nil {
        fmt.Printf("Error converting PORT to integer: %s\n", err)
        return
    }

    endpoint := fmt.Sprintf(":%d", port);
    // Start the server
    router.Run(endpoint)
}
