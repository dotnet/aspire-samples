# Stage 1: Build the Go program
ARG GO_VERSION=1.22
FROM golang:${GO_VERSION} AS builder
WORKDIR /app
COPY . .
RUN go mod tidy
RUN go build .

# Stage 2: Run the Go program
FROM mcr.microsoft.com/cbl-mariner/base/core:2.0
WORKDIR /app
COPY --from=builder /app/ginapp .
CMD ["./ginapp"]
