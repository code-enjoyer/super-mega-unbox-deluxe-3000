# super-mega-unbox-deluxe-3000

## Tests

`api/SuperMegaUnboxDeluxe.Tests` is the single .NET test project for unit,
integration, and other automated backend tests. It uses xUnit and targets .NET 10.
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

Keep tests grouped by area inside this project, such as `Persistence/`, rather
than creating separate test projects. Mark test classes or methods with
`[Trait("Category", "Unit")]` or `[Trait("Category", "Integration")]` so they can
be selected with `--filter`. When adding tests that need external services,
document their setup here and explicitly skip them when that setup is absent.

The current `Persistence/EntityMappingTests.cs` tests validate EF Core entity
mapping and compatibility with the existing migration snapshot. They inspect
the PostgreSQL provider's model without connecting to a database, so they are
categorized as unit tests. No database or Docker setup is needed for these tests;
there are no external-service integration tests in this branch yet.
