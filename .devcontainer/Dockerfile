# syntax=docker/dockerfile:1
ARG VARIANT="8.0-bookworm-slim"
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

RUN apt-get update \
    && apt-get install -y --no-install-recommends openssl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates openssl \
    && curl -fsSL https://raw.githubusercontent.com/dapr/cli/master/install/install.sh | bash \
    && rm -rf /var/lib/apt/lists/*

# Install base tooling required across development, build, test and security scan stages
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    build-essential \
    curl \
    git \
    jq \
    make \
    unzip \
    wget \
    && rm -rf /var/lib/apt/lists/*

# Allow Playwright to install browsers once inside the container when tests require it
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

# Keep parity with local development expectations
ENV ASPNETCORE_ENVIRONMENT=Development \
    NODE_ENV=development
