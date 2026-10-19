using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using SuperMegaUnboxDeluxe.Application.Items.Abstractions;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;
using SuperMegaUnboxDeluxe.Tests.Integration.Api;
using SuperMegaUnboxDeluxe.Tests.TestSupport;
using Testcontainers.PostgreSql;

namespace SuperMegaUnboxDeluxe.Tests.Integration.Infrastructure.Persistence;

[Trait("Category", "Integration")]
[Trait("Category", "Postgres")]
[Collection("API")]
public sealed class DatabaseTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private NpgsqlConnection _admin = null!;
    private readonly string _schema = "smud_tests_" + Guid.NewGuid().ToString("N");
    private string _connectionString = null!;
    private DbContextOptions<SmudDbContext> Options => new DbContextOptionsBuilder<SmudDbContext>().UseNpgsql(_connectionString).Options;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("SMUD_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _container = new PostgreSqlBuilder("postgres:18.6-bookworm").Build();
            try { await _container.StartAsync(); }
            catch
            {
                await _container.DisposeAsync();
                throw new InvalidOperationException("PostgreSQL integration tests require Docker or SMUD_TEST_POSTGRES. Use --filter Category!=Postgres for database-independent tests.");
            }
            connectionString = _container.GetConnectionString();
        }
        _admin = new NpgsqlConnection(connectionString);
        await _admin.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{_schema}\"", _admin);
        await command.ExecuteNonQueryAsync();
        _connectionString = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = _schema, Pooling = false }.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_admin is not null)
        {
            try
            {
                await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE", _admin);
                await command.ExecuteNonQueryAsync();
            }
            finally { await _admin.DisposeAsync(); }
        }
        if (_container is not null) await _container.DisposeAsync();
    }

    [Fact]
    public async Task SmudDbContext_FreshSchemaAndHttpGeneration_ShouldPersistSnapshotsAndReuseSharedBases()
    {
        await using (var database = new SmudDbContext(Options))
        {
            await database.Database.MigrateAsync();
            await database.Database.MigrateAsync();
            database.Database.HasPendingModelChanges().ShouldBeFalse();
            (await database.Database.GetAppliedMigrationsAsync()).ShouldHaveSingleItem().ShouldEndWith("_InitialCreate");
            (await database.Set<ItemBase>().Select(item => item.Id).ToArrayAsync()).ShouldBe(new StarterItemCatalog().Bases.Select(item => item.Id).ToArray(), ignoreOrder: true);
            // Existing nullable snapshots remain representable in the fresh schema.
            var itemBase = await database.Set<ItemBase>().SingleAsync(item => item.Id == StarterItemCatalog.FingerGunsId);
            database.Add(new Item(itemBase, "Snapshot without details", Domain.Enums.ItemRarity.Common, "placeholder",
                DateTimeOffset.UtcNow, 1, [], [new ItemModifier("Legacy description", "+10%")]));
            await database.SaveChangesAsync();
        }

        var random = new SwitchableRandom();
        await using var host = new ApiTestFactory(configure: services => services.AddSingleton<IRandomSource>(random), connectionString: _connectionString);
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        foreach (var source in new StarterItemCatalog().Sources)
        {
            random.Set(81, 0, 3, 40, 1, 0, 2, 15, 3);
            using var created = await client.PostAsJsonAsync("/items", new { lootSourceId = source.Id });
            created.StatusCode.ShouldBe(HttpStatusCode.Created);
            var item = await created.Content.ReadFromJsonAsync<JsonObject>();
            var retrieved = await client.GetFromJsonAsync<JsonObject>(created.Headers.Location);
            JsonNode.DeepEquals(item, retrieved).ShouldBeTrue();
            retrieved!["modifiers"]![1]!["details"]!["effect"]!["id"]!.GetValue<string>().ShouldBe("existential-dread");
        }
        random.Set(100, 0, 30, 100, 50, 2, 10, 15, 3);
        using var uniqueCreated = await client.PostAsync("/items", null);
        uniqueCreated.StatusCode.ShouldBe(HttpStatusCode.Created);
        var unique = await client.GetFromJsonAsync<JsonObject>(uniqueCreated.Headers.Location);
        unique!["rarity"]!.GetValue<string>().ShouldBe("Unique");
        unique["modifiers"]!.AsArray().Count.ShouldBe(2);
        JsonNode.DeepEquals(await uniqueCreated.Content.ReadFromJsonAsync<JsonObject>(), unique).ShouldBeTrue();
        using var serviceScope = host.Services.CreateScope();
        var db = serviceScope.ServiceProvider.GetRequiredService<SmudDbContext>();
        var queries = serviceScope.ServiceProvider.GetRequiredService<IItemQueries>();
        var legacyId = await db.Set<Item>().Where(item => item.Name == "Snapshot without details").Select(item => item.Id).SingleAsync();
        db.ChangeTracker.Clear();
        var snapshot = await queries.GetByIdAsync(legacyId, default);
        snapshot!.Modifiers.ShouldHaveSingleItem().Details.ShouldBeNull();
        db.ChangeTracker.Entries().ShouldBeEmpty();
        (await db.Set<ItemBase>().CountAsync()).ShouldBe(4);
        (await db.Set<Item>().CountAsync()).ShouldBe(4);

        await using var columns = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_schema = @schema AND table_name <> '__EFMigrationsHistory'", _admin);
        columns.Parameters.AddWithValue("schema", _schema);
        await using var reader = await columns.ExecuteReaderAsync();
        while (await reader.ReadAsync()) reader.GetString(0).ShouldMatch("^[a-z][a-z0-9_]*$");
    }

    private sealed class SwitchableRandom : IRandomSource
    {
        private SequenceRandom _random = new();
        public void Set(params int[] values) => _random = new SequenceRandom(values);
        public int NextInclusive(int min, int max) => _random.NextInclusive(min, max);
    }
}
