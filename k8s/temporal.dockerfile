FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y curl ca-certificates && rm -rf /var/lib/apt/lists/*
RUN curl -L "https://github.com/temporalio/cli/releases/download/v1.7.0/temporal_cli_1.7.0_linux_amd64.tar.gz" \
    | tar -xz -C /usr/local/bin temporal
EXPOSE 7233 8233
CMD ["temporal", "server", "start-dev", "--ip", "0.0.0.0", "--port", "7233", "--ui-port", "8233"]
