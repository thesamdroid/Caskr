FROM postgres:17

# Copy only the schema and migration scripts (init.db has table ordering issues)
# Scripts run in alphabetical order: 01-schema-init.sql creates tables first
COPY initdb.d/ /docker-entrypoint-initdb.d/
