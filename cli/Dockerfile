FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm

ARG NODE_VERSION=20
ARG PLAYWRIGHT_VERSION=1.48.2

ENV NODE_VERSION=${NODE_VERSION} \
    PLAYWRIGHT_VERSION=${PLAYWRIGHT_VERSION} \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY cli/install-cli-deps.sh /tmp/install-cli-deps.sh

RUN chmod +x /tmp/install-cli-deps.sh \
    && /tmp/install-cli-deps.sh \
    && rm /tmp/install-cli-deps.sh

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

WORKDIR /workspace
