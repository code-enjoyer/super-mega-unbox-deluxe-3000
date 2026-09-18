# Use official Postgres image as base
FROM postgres:18-alpine

# Set default environment variables (can be overridden by docker-compose)
ENV POSTGRES_USER=smud_user
ENV POSTGRES_PASSWORD=smud_password
ENV POSTGRES_DB=smud_dev

# Expose the default Postgres port
EXPOSE 5432

# Use upstream image entrypoint and default command
CMD ["postgres"]
