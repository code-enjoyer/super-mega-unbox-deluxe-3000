using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using SuperMegaUnboxDeluxe.Infrastructure;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;
using SuperMegaUnboxDeluxe.Tests.Integration.Api;
using SuperMegaUnboxDeluxe.Tests.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace SuperMegaUnboxDeluxe.Tests.Integration.Infrastructure.Persistence;

[Trait("Category", "Integration")]
[Trait("Category", "Postgres")]
public sealed class PostgresTests
{
    [PostgresFact]
    public async Task SmudDbContext_LegacyUpgradeAndHttpGeneration_ShouldPreserveItemsAndReuseSharedBases()
    {
        // Use a unique schema in the supplied database. No CREATEDB permission is needed;
        // migrations and cleanup affect only this test's schema, never the public schema.
        var adminString = Environment.GetEnvironmentVariable("SMUD_TEST_POSTGRES")!;
        var schemaName = "smud_item_tests_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(adminString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schemaName}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            var connectionString = new NpgsqlConnectionStringBuilder(adminString)
            {
                SearchPath = schemaName,
                Pooling = false
            }.ConnectionString;
            var options = new DbContextOptionsBuilder<SmudDbContext>().UseNpgsql(connectionString).Options;
            var legacyBaseId = Guid.NewGuid();
            var legacyItemId = Guid.NewGuid();
            await using (var db = new SmudDbContext(options))
            {
                await db.GetService<IMigrator>().MigrateAsync("20260921131205_AddItems");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO item_bases (\"Id\", \"Name\") VALUES ({legacyBaseId}, 'Sword')");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO items (\"Id\", \"BaseId\", \"Name\", \"Rarity\", \"ImageKey\", \"DateGotten\", \"Value\") VALUES ({legacyItemId}, {legacyBaseId}, 'Excalibur', 'Legendary', 'excalibur', {DateTimeOffset.UtcNow}, 1000)");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO item_modifiers (\"ItemId\", \"Name\", \"Value\") VALUES ({legacyItemId}, 'Critical Hit Chance', '+10%')");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO item_stats (\"ItemId\", \"Type\", \"Value\") VALUES ({legacyItemId}, 'Attack', '100')");
                await db.Database.MigrateAsync();
                Assert.False(db.Database.HasPendingModelChanges());
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SmudDatabase"] = connectionString
            }).Build();
            var random = new SwitchableRandom();
            await using (var host = await TestApiHost.StartAsync(services =>
            {
                services.AddInfrastructure(configuration);
                services.AddSingleton<IRandomSource>(random);
            }))
            {
                var legacy = await host.Client.GetFromJsonAsync<JsonObject>($"/items/{legacyItemId}");
                Assert.Equal("Excalibur", legacy!["name"]!.GetValue<string>());
                Assert.Equal("+10%", legacy["modifiers"]![0]!["value"]!.GetValue<string>());
                Assert.Null(legacy["modifiers"]![0]!["details"]);
                Assert.Equal("100", legacy["stats"]![0]!["value"]!.GetValue<string>());

                foreach (var source in new StarterItemCatalog().Sources)
                {
                    // Rare weapon with numeric bonus and Dread effect; repeated base IDs must not be reinserted.
                    random.Set(81, 0, 3, 40, 1, 0, 2, 15, 3);
                    using var created = await host.Client.PostAsJsonAsync("/items", new { lootSourceId = source.Id });
                    Assert.Equal(HttpStatusCode.Created, created.StatusCode);
                    var item = await created.Content.ReadFromJsonAsync<JsonObject>();
                    var retrieved = await host.Client.GetFromJsonAsync<JsonObject>(created.Headers.Location);
                    Assert.True(JsonNode.DeepEquals(item, retrieved), $"Created: {item}\nRetrieved: {retrieved}");
                    Assert.Equal("existential-dread", retrieved!["modifiers"]![1]!["details"]!["effect"]!["id"]!.GetValue<string>());
                }

                random.Set(100, 0, 30, 100, 50, 2, 10, 15, 3);
                using var uniqueCreated = await host.Client.PostAsync("/items", null);
                Assert.Equal(HttpStatusCode.Created, uniqueCreated.StatusCode);
                var unique = await host.Client.GetFromJsonAsync<JsonObject>(uniqueCreated.Headers.Location);
                Assert.Equal("Unique", unique!["rarity"]!.GetValue<string>());
                Assert.Equal(2, unique["modifiers"]!.AsArray().Count);
                Assert.Equal("Signature", unique["modifiers"]![0]!["details"]!["role"]!.GetValue<string>());
                Assert.True(JsonNode.DeepEquals(await uniqueCreated.Content.ReadFromJsonAsync<JsonObject>(), unique));
            }

            await using (var db = new SmudDbContext(options))
            {
                Assert.Equal(5, await db.Set<ItemBase>().CountAsync()); // four canonical bases plus the untouched legacy base
                Assert.Equal(4, await db.Set<Item>().CountAsync());
                Assert.Equal(2, await db.Set<Item>().CountAsync(x => x.Base.Id == StarterItemCatalog.FingerGunsId));
            }
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schemaName}\" CASCADE", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private sealed class SwitchableRandom : IRandomSource
    {
        private SequenceRandom _random = new();
        public void Set(params int[] values) => _random = new SequenceRandom(values);
        public int NextInclusive(int min, int max) => _random.NextInclusive(min, max);
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMUD_TEST_POSTGRES")))
            Skip = "Set SMUD_TEST_POSTGRES to a test-database connection with CREATE SCHEMA permission.";
    }
}
