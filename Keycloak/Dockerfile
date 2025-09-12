FROM quay.io/keycloak/keycloak:22.0
ENV KEYCLOAK_ADMIN=admin
ENV KEYCLOAK_ADMIN_PASSWORD=admin
# The base Keycloak image is built on top of UBI micro which doesn't include a
# package manager. Installing additional tools such as curl via microdnf isn't
# possible in this environment. Instead we download a pre-built static curl
# binary directly and mark it as executable.
ADD https://github.com/moparisthebest/static-curl/releases/download/v8.5.0/curl-amd64 /usr/bin/curl
RUN chmod +x /usr/bin/curl
ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
CMD ["start-dev"]
