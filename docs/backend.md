# Backend conventions

Use .NET 10, ASP.NET Core controllers, and the six existing projects. Common has
small shared technical types; Domain holds game rules without HTTP/EF/logging
dependencies; Application orchestrates use cases and defines context-specific
abstractions; Infrastructure implements persistence and external adapters; Api
composes services and translates HTTP contracts; Tests exercises these boundaries.

Compiler conventions live in `Directory.Build.props`; package versions live in
`Directory.Packages.props`. Project files declare their own dependencies. Keep
nullable enabled, explicit imports, pinned tools, and committed package lockfiles.
Apply the backend EditorConfig; avoid unrelated formatting in other components.

Application item features contain commands/queries and directly injected handlers
using `HandleAsync(request, cancellationToken)`. Context-specific repositories and
query interfaces live under `Items/Abstractions`. Add a mediator only when repeated
cross-cutting handler behavior warrants one. No empty feature scaffolding is needed.

Handlers explicitly invoke FluentValidation `ValidateAsync`; domain behavior
independently protects its rules. Expected validation/not-found outcomes use small
Common results/errors. Api maps them centrally to Problem Details; unexpected
failures remain exceptions and are logged once at the HTTP boundary. Controllers
map Application-owned read models to separate HTTP contracts manually.

Commands coordinate the aggregate change and one unit-of-work save. Queries project
read models without tracking. Pass cancellation through I/O. Keep EF mapping in
Infrastructure and define table, column, key, index, and constraint names in
snake_case. The EF migration-history table retains its framework convention.
API JSON remains camelCase; acquisition timestamps are UTC at PostgreSQL
microsecond precision. Existing item numeric types and JSON modifier snapshots
remain part of the game's contract.

Use `ILogger<T>` in Application/Infrastructure. Serilog exports structured logs;
OpenTelemetry exports traces and metrics. Logs have only one OTLP export path.
Keep trace/span IDs and correlation IDs; avoid credentials, full configuration,
and high-cardinality metric labels. Domain remains telemetry-independent.

See [development](development.md) for tests and [service operations](../deploy/README.md)
for configuration, migrations, and telemetry.
