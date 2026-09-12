using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SuperMegaUnboxDeluxe.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ItemsController
{
    [HttpPost(Name = "GenerateItem")]
    public async Task<string> GenerateItemAsync([FromBody] ItemRequest? request, CancellationToken cancellationToken)
    {
        var item = new Item
        {
            ItemBase = "Sword",
            Name = "Excalibur",
            Stats = new[] { "Attack: 100", "Durability: 80" },
            Modifiers = new[] { "+10% Critical Hit Chance", "+5% Attack Speed" },
            Rarity = "Legendary",
            ImageKey = "excalibur",
            DateGotten = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Value = "1000 Gold"
        };

        return JsonSerializer.Serialize(item);
    }

    public class ItemRequest
    {
        public string? ItemType { get; init; } = null;
    }

    public class Item
    {
        public string ItemBase { get; init; }
        public string Name { get; init; }
        public string[] Stats { get; init; }
        public string[] Modifiers { get; init; }
        public string Rarity { get; init; }
        public string ImageKey { get; init; }
        public string DateGotten { get; init; }
        public string Value { get; init; }
    }
}
