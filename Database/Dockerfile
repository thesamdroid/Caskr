FROM postgres:17

# Copy database dump and initialization scripts
COPY init.db /docker-entrypoint-initdb.d/00-init.sql
COPY initdb.d/ /docker-entrypoint-initdb.d/
