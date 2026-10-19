#!/bin/sh
(
set -eu
# Only runs on an empty volume. psql quotes the password as a SQL literal.
psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set ON_ERROR_STOP=1 <<'SQL'
\getenv app_password SMUD_DB_PASSWORD
CREATE ROLE smud_user LOGIN PASSWORD :'app_password';
CREATE DATABASE smud_dev OWNER smud_user;
REVOKE ALL ON DATABASE smud_dev FROM PUBLIC;
SQL
)
