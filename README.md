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
these tests; there are no external-service integration tests in this branch yet.
