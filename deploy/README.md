# Local services

Run commands from the repository root. `./dev.ps1 setup -Scope services` copies
the ready-to-run defaults from `.env.example` into ignored `.env.local` when it
is missing. The example administrator password is `postgres`, the application
password is `smud`, and the OTLP key is `smud-local-development-otlp-key`.
These defaults are for the loopback-only local stack. Optional private overrides
stay in `.env.local`; `./dev.ps1 check -Scope services` validates configuration
without displaying resolved values.

The Compose project remains `super-mega-unbox-deluxe-3000`. All published ports
bind to loopback. PostgreSQL has a separate administrator and the application
login `smud_user`, which owns `smud_dev` and is not a superuser. Initialization
runs only on an empty volume. Changing a password in `.env.local` does not rotate
an existing database role's password.
The optional `POSTGRES_PORT` setting defaults to `5433`. Set it to an available
port, such as `5434`, if a native PostgreSQL service already occupies that port;
use the same value for host-run API connections. Compose services still use `5432`.

## New migration history and development reset

The former migrations were replaced outright by one snake_case `InitialCreate`
migration because there is no production database. Old development databases are
incompatible and must be rebuilt. The reset **discards existing development items**:

```powershell
./dev.ps1 reset-db
```

This checks ownership of exactly
`super-mega-unbox-deluxe-3000_postgres_data`, stops this project's stack, removes
only that volume, and rebuilds/starts the stack. The migration job creates the
new schema and canonical seed rows before the API starts. Ordinary `setup`,
`dev`, and `stop` commands retain volumes. Other projects' volumes are untouched.

For routine operation:

```powershell
./dev.ps1 dev
docker compose --env-file .env.local -f deploy/compose.yaml ps --all
./dev.ps1 stop
```

## Endpoints and telemetry

| Service | Host endpoint | Compose endpoint |
| --- | --- | --- |
| PostgreSQL | `127.0.0.1:5433` | `postgres:5432` |
| API | `http://127.0.0.1:5000` | `http://api:8080` |
| Client | `http://127.0.0.1:5173` | `http://client:80` |
| Aspire UI | `http://127.0.0.1:18898` | `http://aspire:18888` |
| OTLP HTTP/protobuf | `http://127.0.0.1:4328/` | `http://aspire:18890/` |

Aspire retains browser-token protection. Retrieve its browser token privately
from the Aspire container's Logs tab in Docker Desktop. OTLP uses a separate
`x-otlp-api-key` header from `.env.local`; the UI token is not that key. Do not
dump Aspire logs into shared terminals or chat. Dashboard history is ephemeral.

The Compose API exports logs through Serilog alone and traces/metrics through
OpenTelemetry. Host telemetry is optional; see [development](../docs/development.md).
Send a request to `/api/status`, then inspect its request log, trace, database/HTTP
spans where applicable, and runtime metrics in Aspire. Exporter traffic is excluded
from self-instrumentation. A running dashboard container alone does not establish
successful telemetry ingestion.

## Migrations and diagnostics

Normal API startup never migrates. Compose runs a one-shot `api-migrate` job;
nonzero exit prevents the API from starting. A host configured through User Secrets
can run the same command or the pinned EF tool:

```powershell
dotnet run --project backend/Api -- --migrate
dotnet tool restore
dotnet ef database update --project backend/Infrastructure --startup-project backend/Api -- --environment Development
dotnet ef migrations add NextChange --project backend/Infrastructure --startup-project backend/Api --output-dir Persistence/Migrations -- --environment Development
dotnet ef migrations has-pending-model-changes --project backend/Infrastructure --startup-project backend/Api -- --environment Development
```

`/api/status` does not query PostgreSQL. `/health` reports 503 on a database outage
and returns to healthy when connectivity recovers. API containers run as a non-root
user; daily Serilog files under `/app/logs` are writable and retained for 14 days
inside the container. Use Docker Desktop to inspect failed services without
printing private configuration. Stop/start retains data; container recreation does
not retain log files.

The backend image restores only the API dependency graph using package lockfiles.
For a direct image build, use `docker build -f deploy/api/Dockerfile -t smud-api .`.
