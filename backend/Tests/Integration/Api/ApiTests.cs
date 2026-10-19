using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Application.Items;
using SuperMegaUnboxDeluxe.Application.Items.Abstractions;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using SuperMegaUnboxDeluxe.Tests.TestSupport;

namespace SuperMegaUnboxDeluxe.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("API")]
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
            services.AddSingleton<IItemQueries>(store);
            services.AddSingleton<IUnitOfWork>(store);
            services.AddSingleton<IRandomSource>(new SequenceRandom(81, 0, 3, 40, 1, 0, 2, 15, 3));
        });
        var client = host.Client;
        var sources = await client.GetFromJsonAsync<JsonArray>("/loot-sources");
        sources!.Count.ShouldBe(2);
        (sources[0]!["id"]!.GetValue<string>()).ShouldBe("suspicious-lost-property");
        (sources[1]!["id"]!.GetValue<string>()).ShouldBe("definitely-legal-arms-dealer");

        using var created = await client.PostAsync("/items", null);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        created.Headers.Location.ShouldNotBeNull();
        var item = await created.Content.ReadFromJsonAsync<JsonObject>();
        (item!["rarity"]!.GetValue<string>()).ShouldBe("Rare");
        (item["value"]!.GetValue<int>()).ShouldBe(203);
        var details = item["modifiers"]![1]!["details"]!;
        (details["role"]!.GetValue<string>()).ShouldBe("Suffix");
        (details["effect"]!["trigger"]!.GetValue<string>()).ShouldBe("OnHit");
        (details["effect"]!["chancePercent"]!.GetValue<int>()).ShouldBe(15);
        var retrieved = await client.GetFromJsonAsync<JsonObject>(created.Headers.Location);
        (JsonNode.DeepEquals(item, retrieved)).ShouldBeTrue();

        using var chosenSource = await client.PostAsJsonAsync("/items", new { lootSourceId = "definitely-legal-arms-dealer" });
        chosenSource.StatusCode.ShouldBe(HttpStatusCode.Created);
        store.Items.Count.ShouldBe(2);
        using var unknown = await client.PostAsJsonAsync("/items", new { lootSourceId = "missing" });
        unknown.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        unknown.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var malformed = await client.PostAsync("/items", new StringContent("{", Encoding.UTF8, "application/json"));
        malformed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var wrongType = await client.PostAsJsonAsync("/items", new { lootSourceId = 123 });
        wrongType.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var missing = await client.GetAsync($"/items/{Guid.NewGuid()}");
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        store.Items.Count.ShouldBe(2);
    }

    private sealed class MemoryStore : IItemBaseRepository, IItemRepository, IItemQueries, IUnitOfWork
    {
        public ConcurrentDictionary<Guid, Item> Items { get; } = new();
        public void Add(Item item) => (Items.TryAdd(item.Id, item)).ShouldBeTrue();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
        Task<ItemDetails?> IItemQueries.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.TryGetValue(id, out var item) ? ItemDetails.FromItem(item) : null);
        Task<ItemBase?> IItemBaseRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var definition = new StarterItemCatalog().Bases.Single(x => x.Id == id);
            return Task.FromResult<ItemBase?>(new ItemBase(id, definition.Name));
        }
    }
}
