# Combined environment for Keycloak and PostgreSQL database with seeded data
FROM postgres:15-alpine

# Install dependencies for Keycloak
RUN apk add --no-cache openjdk17-jdk curl powershell

# Download and set up Keycloak
ENV KEYCLOAK_VERSION=22.0.0 \
    KEYCLOAK_ADMIN=admin \
    KEYCLOAK_ADMIN_PASSWORD=admin
RUN curl -fsSL https://github.com/keycloak/keycloak/releases/download/$KEYCLOAK_VERSION/keycloak-$KEYCLOAK_VERSION.tar.gz \
    | tar -xz -C /opt && mv /opt/keycloak-$KEYCLOAK_VERSION /opt/keycloak

# Configure Postgres defaults
ENV POSTGRES_USER=postgres \
    POSTGRES_PASSWORD=docker \
    POSTGRES_DB=caskr-db

# Copy database seed scripts
COPY Database/initdb.d /docker-entrypoint-initdb.d

# Copy startup script
COPY start-services.ps1 /start-services.ps1
RUN chmod +x /start-services.ps1

EXPOSE 5432 8080

CMD ["pwsh", "/start-services.ps1"]
