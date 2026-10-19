# super-mega-unbox-deluxe-3000

## Tests

`api/SuperMegaUnboxDeluxe.Tests` is the single project for all automated tests,
including unit and integration tests. It uses xUnit and targets .NET 10.
It references the API project, giving tests access to the application, domain,
and infrastructure projects as well as ASP.NET Core hosting APIs.

Run all tests from the repository root with the .NET 10 SDK installed:

```powershell
dotnet test api/SuperMegaUnboxDeluxe.Api.slnx
```

To run only this project or its unit tests:

```powershell
dotnet test api/SuperMegaUnboxDeluxe.Tests/SuperMegaUnboxDeluxe.Tests.csproj
dotnet test api/SuperMegaUnboxDeluxe.Tests/SuperMegaUnboxDeluxe.Tests.csproj --filter "Category=Unit"
```

Keep all automated tests in this project. Use a root folder for each
test type, such as `Unit/` or `Integration/`. Beneath that, generally mirror the
source project and its folder structure where applicable, using the project
suffix (`Api`, `Application`, `Domain`, or `Infrastructure`) as the project folder.
For example, tests for
`SuperMegaUnboxDeluxe.Infrastructure/Persistence/SmudDbContext.cs` live in:

```text
SuperMegaUnboxDeluxe.Tests/
  Unit/
    Infrastructure/
      Persistence/
        SmudDbContextTests.cs
```

Match test namespaces to their folder paths. Mark test classes or methods with
`[Trait("Category", "Unit")]` or `[Trait("Category", "Integration")]` to match
their test-type folder and allow selection with `--filter`. When adding tests
that need external services, document their setup here and explicitly skip them
when that setup is absent.

Name test methods using `{SystemUnderTest}_{Scenario}_Should{ExpectedOutcome}()`
with PascalCase within each part. For example:

```csharp
SmudDbContext_ExistingMappings_ShouldMatchMigrationSnapshot()
SmudDbContext_EntityWithoutIndividualConfiguration_ShouldApplySharedMapping()
```

The current `Unit/Infrastructure/Persistence/SmudDbContextTests.cs` tests validate
EF Core entity mapping and compatibility with the existing migration snapshot.
They inspect the PostgreSQL provider's model without connecting to a database,
so they are categorized as unit tests. No database or Docker setup is needed for
these tests. Service and generation unit tests live under `Unit/Application/Items`
and `Unit/Domain/Items/Generation`. HTTP contract tests live under `Integration/Api`
and start a local host with in-memory persistence; no database is needed. PostgreSQL
migration and HTTP round-trip tests live under `Integration/Infrastructure/Persistence`
and require the opt-in database setup described below. Shared deterministic random
draws live in `TestSupport/SequenceRandom.cs`.

## Item generation MVP

Choose a loot source and open it for free. Items are generated on the server,
saved to PostgreSQL, and returned with rolled stats, affixes, and descriptive
special effects. There is no inventory, combat execution, or opening cost yet.

### Where things live

| Project / folder | Responsibility |
| --- | --- |
| `Domain/Items/Generation` | Immutable definition types, catalog validation, random rolls, naming, stat contributions, value, and item construction. No database or HTTP access. |
| `Application/Items/Catalog` | `StarterItemCatalog`: edit the bases, affixes, Unique definitions, loot sources, ranges, and weights here. |
| `Application/Items` | Resolve a source, generate a roll, load its canonical base, and save the item once. |
| `Infrastructure/Persistence` | EF mappings, repositories, migrations, canonical base seed rows, and JSON modifier snapshots. |
| `Infrastructure/Items` | Production random-number adapter; tests substitute deterministic draws. |
| `Api/Controllers` and `Api/Contracts` | HTTP validation, response mapping, generation/detail endpoints, and source summaries. |
| `client/src/components/item` | Item response types and the item-details card. |

Project folders above are inside `api/SuperMegaUnboxDeluxe.<Project>`.
The Domain does not depend on the other projects; Application depends on Domain,
Infrastructure implements Application's persistence interfaces, and API composes them.

### Content and rules

Starter bases are Finger Guns, Petrified Salamander, Turtle-Shell Shield, and
Suspicious Trench Coat. Sir Newt-on, the Unreasonably Rigid is an authored Unique
salamander with a signature effect and a 50% chance of an additional affix.

| Source | Common | Uncommon | Rare | Epic | Legendary | Unique |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Suspicious Lost Property (weapons and armour) | 55% | 25% | 12% | 5% | 2% | 1% |
| Definitely Legal Arms Dealer (weapons) | 35% | 30% | 20% | 10% | 4% | 1% |

Common items have no affixes; Uncommon has one prefix or suffix; Rare through
Legendary have both. Only compatible affixes can roll. Numeric bonuses are already
included in the displayed stat totals. Special effects contain a stable ID,
trigger, chance, and optional duration/count, alongside readable text.

Rarity scales Damage/Defense by 1 / 1.2 / 1.5 / 2 / 3; Durability is not scaled.
Unique items use their authored ranges. Value is `(Damage/Defense * 10 + Durability
+ 25 per special effect) * rarity multiplier`, with Unique using 4. Numeric rolls
use inclusive bounds; rounding uses midpoint away from zero. Unique bonus affixes
do not change the authored item name.

Catalog content is validated at startup. Generated values and modifier details are
snapshots: editing the catalog does not reroll existing items. Base GUIDs are stable;
adding or renaming a canonical base requires an EF migration as well as a catalog edit.
Legacy modifiers remain readable with `details: null`.

### HTTP and client

- `GET /loot-sources` returns source IDs, names, and descriptions.
- `POST /items` accepts `{ "lootSourceId": "definitely-legal-arms-dealer" }`.
  An omitted body/source uses `suspicious-lost-property`. Unknown source IDs return
  `400` Problem Details; successful creation returns `201` and a `Location` header.
- `GET /items/{id}` returns the saved snapshot, or `404`.

The previous `itemType` request field has been replaced by `lootSourceId`.
The React client defaults to `http://localhost:5000`; `VITE_API_URL` can override it
at build time. Artwork currently uses a placeholder: `imageKey` is an identifier,
not a URL. Effects are descriptions/data, not executed gameplay.

Apply migrations through the API's configured `Api:AutoApplyMigrations` startup
option or `dotnet ef database update --project api/SuperMegaUnboxDeluxe.Infrastructure
--startup-project api/SuperMegaUnboxDeluxe.Api` with the correct database connection.
The API Docker build restores only the API dependency graph, not the test project.

### Verification

```powershell
dotnet test api/SuperMegaUnboxDeluxe.Api.slnx
cd client
npm run build
npm run lint
```

The PostgreSQL integration test is opt-in: set `SMUD_TEST_POSTGRES` to a connection
string for the running development/test database before `dotnet test`. It needs
permission to create a schema, not a database. It creates a uniquely named schema,
tests upgrading legacy data and HTTP creation/retrieval, then drops only that schema.
Without the variable, that test is explicitly skipped. Never point it at production.

For Docker-only verification, build the API Dockerfile's `build` target as a test
image and run `dotnet test` in it on the Compose network, passing `SMUD_TEST_POSTGRES`
with `Host=postgres;Port=5432` and your configured database credentials. This also
avoids accidentally connecting to a native PostgreSQL service sharing the published
host port. `docker compose up -d --build api client` rebuilds the playable app and,
with development auto-migrations enabled, applies the migration inside Docker.
