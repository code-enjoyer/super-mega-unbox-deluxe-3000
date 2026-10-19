using Microsoft.Extensions.DependencyInjection;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Application.Repositories;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using SuperMegaUnboxDeluxe.Tests.TestSupport;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace SuperMegaUnboxDeluxe.Tests.Integration.Api;

[Trait("Category", "Integration")]
public sealed class ApiTests
{
    [Fact]
    public async Task Api_ItemGenerationRequests_ShouldSupportSourcesCreationRetrievalAndValidation()
    {
        var store = new MemoryStore();
        await using var host = await TestApiHost.StartAsync(services =>
        {
            services.AddSingleton<IItemBaseRepository>(store);
            services.AddSingleton<IItemRepository>(store);
            services.AddSingleton<IUnitOfWork>(store);
            services.AddSingleton<IRandomSource>(new SequenceRandom(81, 0, 3, 40, 1, 0, 2, 15, 3));
        });
        var client = host.Client;
        var sources = await client.GetFromJsonAsync<JsonArray>("/loot-sources");
        Assert.Equal(2, sources!.Count);
        Assert.Equal("suspicious-lost-property", sources[0]!["id"]!.GetValue<string>());
        Assert.Equal("definitely-legal-arms-dealer", sources[1]!["id"]!.GetValue<string>());

        using var created = await client.PostAsync("/items", null);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var item = await created.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Rare", item!["rarity"]!.GetValue<string>());
        Assert.Equal(203, item["value"]!.GetValue<int>());
        var details = item["modifiers"]![1]!["details"]!;
        Assert.Equal("Suffix", details["role"]!.GetValue<string>());
        Assert.Equal("OnHit", details["effect"]!["trigger"]!.GetValue<string>());
        Assert.Equal(15, details["effect"]!["chancePercent"]!.GetValue<int>());
        var retrieved = await client.GetFromJsonAsync<JsonObject>(created.Headers.Location);
        Assert.True(JsonNode.DeepEquals(item, retrieved));

        using var chosenSource = await client.PostAsJsonAsync("/items", new { lootSourceId = "definitely-legal-arms-dealer" });
        Assert.Equal(HttpStatusCode.Created, chosenSource.StatusCode);
        Assert.Equal(2, store.Items.Count);
        using var unknown = await client.PostAsJsonAsync("/items", new { lootSourceId = "missing" });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal("application/problem+json", unknown.Content.Headers.ContentType!.MediaType);
        using var malformed = await client.PostAsync("/items", new StringContent("{", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        using var wrongType = await client.PostAsJsonAsync("/items", new { lootSourceId = 123 });
        Assert.Equal(HttpStatusCode.BadRequest, wrongType.StatusCode);
        using var missing = await client.GetAsync($"/items/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(2, store.Items.Count);
    }

    private sealed class MemoryStore : IItemBaseRepository, IItemRepository, IUnitOfWork
    {
        public ConcurrentDictionary<Guid, Item> Items { get; } = new();
        public void Add(Item item) => Assert.True(Items.TryAdd(item.Id, item));
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
        Task<Item?> IItemRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.GetValueOrDefault(id));
        Task<ItemBase?> IItemBaseRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var definition = new StarterItemCatalog().Bases.Single(x => x.Id == id);
            return Task.FromResult<ItemBase?>(new ItemBase(id, definition.Name));
        }
    }
}
