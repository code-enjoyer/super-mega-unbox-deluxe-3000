# Backend development

Use PowerShell 7, the .NET SDK pinned in `global.json`, and Docker Desktop with
Linux containers. The backend is `backend/SuperMegaUnboxDeluxe.slnx`.

```powershell
./dev.ps1 setup -Scope backend
./dev.ps1 setup -Scope services
```

Setup copies the ready-to-run development defaults from `.env.example` to ignored
`.env.local` only when that file is missing. The administrator password is
`postgres`, the application password is `smud`, and the OTLP key is
`smud-local-development-otlp-key`. These are example values for the loopback-only
local stack. Existing local settings are never overwritten by setup. Custom OTLP
keys must contain at least 22 URL-safe characters; single-quote dotenv passwords
to preserve special characters.
If another PostgreSQL instance uses port `5433`, set `POSTGRES_PORT=5434` in
`.env.local` and use that port for host connections. The container port stays `5432`.

```powershell
./dev.ps1 check -Scope services
./dev.ps1 dev
./dev.ps1 stop
```

`dev` builds and starts PostgreSQL, the migration job, API, client, and standalone
Aspire dashboard. The API waits for successful migrations; the client waits for
API health. `stop` retains saved volumes. See [service operations](../deploy/README.md)
for the one-time database reset required by the new migration history.

For Visual Studio F5 or `dotnet run --project backend/Api --launch-profile https`,
configure `ConnectionStrings:SmudDatabase` in the API project's **Manage User
Secrets** editor. Its host connection uses `127.0.0.1:5433`, database `smud_dev`,
and application user `smud_user`. Use Npgsql quoting when a password includes
delimiters. User Secrets are loaded only in Development; environment variables
and command-line arguments retain normal ASP.NET precedence. Never pass passwords
on a command line or put them in launch settings. Existing User Secrets remain
associated with the same project ID. .NET does not load `.env.local`.

The host profiles retain HTTP `5077` and HTTPS `7119`; Compose retains API `5000`
and client `5173`. `/scalar` is available when `Api:EnableOpenApi` is true.
`/api/status` checks API startup; `/health` checks PostgreSQL connectivity.

Host telemetry is optional and defaults to disabled. Enable `Telemetry:Enabled`
and set `Telemetry:ApiKey` privately in User Secrets to the same key as the
dashboard. The default HTTP/protobuf endpoint is `http://127.0.0.1:4328/`.

## Verification

```powershell
./dev.ps1 check -Scope backend
./dev.ps1 test -Scope backend -TestFilter 'Category=Unit'
./dev.ps1 test -Scope backend -TestFilter 'Category!=Postgres'
pwsh -NoProfile -File tests/dev.Tests.ps1
```

After building the Compose images, `pwsh -NoProfile -File tests/services.Smoke.ps1`
checks the running containers using disposable fixture settings, random loopback
ports, and a separate randomly named Compose project. It verifies migration
success/failure, persistence, database privileges, non-root logging, and dashboard
authentication, then removes only its own containers and volume. It does not read
`.env.local` or reset the development database.

Backend checks run locked restore, formatting verification, Release build, and
tests. `setup` restores dependencies and local EF tools; intentional dependency
updates regenerate the committed package lockfiles. Direct equivalents are:

```powershell
dotnet restore backend/SuperMegaUnboxDeluxe.slnx --locked-mode
dotnet format backend/SuperMegaUnboxDeluxe.slnx --verify-no-changes --no-restore
dotnet build backend/SuperMegaUnboxDeluxe.slnx -c Release --no-restore
dotnet test backend/SuperMegaUnboxDeluxe.slnx -c Release --no-build --no-restore
```

All tests live in one project, organized by kind and source context. Use xUnit,
Shouldly assertions, and NSubstitute for interface substitutions. Name tests
`{SystemUnderTest}_{Scenario}_Should{ExpectedOutcome}` and label Unit/Integration
categories. HTTP tests exercise the production composition root through
WebApplicationFactory, without developer secrets. Serilog hosts run sequentially.

PostgreSQL tests additionally have `Category=Postgres`. They start disposable
PostgreSQL 18 containers by default; Docker must be running. `SMUD_TEST_POSTGRES`
can override this with a private test connection requiring CREATE SCHEMA permission.
Each test uses a uniquely named schema and drops only that schema. Never use a
production database. Tests fail clearly if neither Docker nor an external test
database is available. The database-independent filter above needs neither.

The client remains independently runnable from `client/` using its existing npm
commands. Backend conventions are described in [backend.md](backend.md).
