FROM alpine:3.22.1 AS download
ARG TARGETARCH
RUN apk add --no-cache curl xz
WORKDIR /download
RUN case "$TARGETARCH" in \
        arm64) target=aarch64; checksum=b85f6bbfd47ca3f04065aee0cb92ee04cef0a234c8a2971ee2a6fc81278675ad ;; \
        amd64) target=x86_64; checksum=3a813dcbed2b28a4438f9cb777566d8c06f026aa74e0623706ca4a2c48e2a8b4 ;; \
        *) echo "Unsupported architecture: $TARGETARCH" >&2; exit 1 ;; \
    esac \
    && curl --fail --show-error --location \
        "https://github.com/LucasPickering/slumber/releases/download/v5.3.0/slumber-${target}-unknown-linux-musl.tar.xz" \
        --output slumber.tar.xz \
    && echo "$checksum  slumber.tar.xz" | sha256sum -c - \
    && tar -xJf slumber.tar.xz --strip-components=1

FROM alpine:3.22.1
RUN apk add --no-cache ca-certificates vim \
    && adduser -D -u 10001 slumber
COPY --from=download /download/slumber /usr/local/bin/slumber
COPY --from=download /download/LICENSE /usr/local/share/licenses/slumber/LICENSE
COPY --chown=slumber:slumber slumber.yml /home/slumber/slumber.yml
COPY --chown=slumber:slumber config.yml /home/slumber/.config/slumber/config.yml
USER slumber
WORKDIR /home/slumber
ENV TERM=xterm-256color COLORTERM=truecolor EDITOR=vim VISUAL=vim
ENTRYPOINT ["slumber", "--file", "/home/slumber/slumber.yml"]
