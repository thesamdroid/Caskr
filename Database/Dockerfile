FROM postgres:17

# Set default database, user, and password
ENV POSTGRES_DB=caskr \
    POSTGRES_USER=caskr \
    POSTGRES_PASSWORD=caskr

# Copy initialization scripts
COPY initdb.d/ /docker-entrypoint-initdb.d/
