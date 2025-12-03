FROM quay.io/keycloak/keycloak:21.1.1 AS builder

# Build optimized Keycloak
WORKDIR /opt/keycloak
ENV KC_DB=postgres
RUN /opt/keycloak/bin/kc.sh build

FROM quay.io/keycloak/keycloak:21.1.1

# Copy optimized build
COPY --from=builder /opt/keycloak/ /opt/keycloak/

# Copy realm configuration
COPY realm-export.json /opt/keycloak/data/import/realm-export.json

# Environment variables for configuration
ENV KC_DB=postgres
ENV KC_HEALTH_ENABLED=true
ENV KC_METRICS_ENABLED=true

ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]

# Default to dev mode; override with start for production
# Production: CMD ["start", "--optimized", "--import-realm"]
CMD ["start-dev", "--import-realm"]
