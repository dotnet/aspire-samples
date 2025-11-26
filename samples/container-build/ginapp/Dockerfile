# Stage 1: Build the Go program
ARG GO_VERSION=1.25.4
FROM golang:${GO_VERSION} AS builder
WORKDIR /app

# Copy go mod files first and download dependencies (cached layer)
COPY go.mod go.sum ./
RUN go mod download

# Copy source code and tidy dependencies
COPY . .
RUN go mod tidy
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-s -w" -o ginapp .

# Stage 2: Run the Go program
FROM mcr.microsoft.com/cbl-mariner/distroless/minimal:2.0
WORKDIR /app
COPY --from=builder /app/ginapp .
CMD ["./ginapp"]
