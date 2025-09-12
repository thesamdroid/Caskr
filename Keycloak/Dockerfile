FROM quay.io/keycloak/keycloak:22.0
ENV KEYCLOAK_ADMIN=admin
ENV KEYCLOAK_ADMIN_PASSWORD=admin
RUN microdnf install -y curl && microdnf clean all
ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
CMD ["start-dev"]
