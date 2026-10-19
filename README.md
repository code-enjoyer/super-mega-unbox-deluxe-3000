# super-mega-unbox-deluxe-3000

## Backend

The .NET 10 solution is `backend/SuperMegaUnboxDeluxe.slnx`, containing Api,
Application, Common, Domain, Infrastructure, and Tests projects.

- [Development and tests](docs/development.md)
- [Backend conventions](docs/backend.md)
- [Local services, database reset, migrations, and telemetry](deploy/README.md)

```powershell
./dev.ps1 setup -Scope backend
./dev.ps1 check -Scope backend
```

The new initial migration replaces the old history. Existing development databases
need the explicit `./dev.ps1 reset-db` workflow after configuring local settings.
This discards development items. Ordinary start/stop commands retain data.

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

Project folders above are inside `backend/<Project>`.
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

Apply migrations through the explicit `--migrate` command or the pinned EF tool;
normal API startup never migrates. Compose runs migrations before API startup.
See [service operations](deploy/README.md) for setup and commands.

### Verification

```powershell
dotnet test backend/SuperMegaUnboxDeluxe.slnx
cd client
npm run build
npm run lint
```

PostgreSQL integration tests use disposable Testcontainers by default and require
Docker. `SMUD_TEST_POSTGRES` optionally supplies a private test database; tests create
and drop only isolated schemas. Use `--filter 'Category!=Postgres'` to run without
Docker. See [development](docs/development.md) for test organization and details.
