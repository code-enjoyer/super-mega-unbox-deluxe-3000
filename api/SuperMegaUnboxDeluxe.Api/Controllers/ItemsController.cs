using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SuperMegaUnboxDeluxe.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ItemsController : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetItemDetails")]
    public async Task<ActionResult<Item>> GetItemDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = new Item
        {
            Id = id,
            ItemBase = "Sword",
            Name = "Excalibur",
            Stats = new[] { "Attack: 100", "Durability: 80" },
            Modifiers = new[] { "+10% Critical Hit Chance", "+5% Attack Speed" },
            Rarity = "Legendary",
            ImageKey = "excalibur",
            DateGotten = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Value = "1000 Gold"
        };

        return Ok(item);
    }

    [HttpPost(Name = "GenerateItem")]
    public async Task<ActionResult<Item>> GenerateItemAsync([FromBody] ItemRequest? request, CancellationToken cancellationToken)
    {
        var item = new Item
        {
            Id = Guid.NewGuid(),
            ItemBase = "Sword",
            Name = "Excalibur",
            Stats = new[] { "Attack: 100", "Durability: 80" },
            Modifiers = new[] { "+10% Critical Hit Chance", "+5% Attack Speed" },
            Rarity = "Legendary",
            ImageKey = "excalibur",
            DateGotten = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Value = "1000 Gold"
        };

        return CreatedAtAction("GetItemDetails",
            new { id = item.Id },
            item);
    }

    public class ItemRequest
    {
        public string? ItemType { get; init; } = null;
    }

    public class Item
    {
        public Guid Id { get; set; }
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
