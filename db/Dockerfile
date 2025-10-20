# Use official PostgreSQL image
FROM postgres:15-alpine

# Set default environment variables (can be overridden in docker-compose.yml)
ENV POSTGRES_DB=PIPlanningDB
ENV POSTGRES_USER=postgres
ENV POSTGRES_PASSWORD=YourStrong!Passw0rd

# Optional: copy init SQL scripts
# Any .sql or .sh files in /docker-entrypoint-initdb.d will be run on container startup
COPY init.sql /docker-entrypoint-initdb.d/

# Expose PostgreSQL port
EXPOSE 5432
