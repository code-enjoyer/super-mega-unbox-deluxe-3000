using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            Stats = new[] { new Item.ItemStat { Name = "Attack", Value = "100" }, new Item.ItemStat { Name = "Durability", Value = "80" } },
            Modifiers = new[] { new Item.ItemModifier { Name = "Critical Hit Chance", Value = "+10%" }, new Item.ItemModifier { Name = "Attack Speed", Value = "+5%" } },
            Rarity = "Legendary",
            ImageKey = "excalibur",
            DateGotten = DateTimeOffset.UtcNow,
            Value = 1000.0f
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
            Stats = new[] { new Item.ItemStat { Name = "Attack", Value = "100" }, new Item.ItemStat { Name = "Durability", Value = "80" } },
            Modifiers = new[] { new Item.ItemModifier { Name = "Critical Hit Chance", Value = "+10%" }, new Item.ItemModifier { Name = "Attack Speed", Value = "+5%" } },
            Rarity = "Legendary",
            ImageKey = "excalibur",
            DateGotten = DateTimeOffset.UtcNow,
            Value = 1000.0f
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
        public required string ItemBase { get; init; }
        public required string Name { get; init; }
        public required ItemStat[] Stats { get; init; }
        public required ItemModifier[] Modifiers { get; init; }
        public required string Rarity { get; init; }
        public required string ImageKey { get; init; }
        public DateTimeOffset DateGotten { get; init; }
        public float Value { get; init; }

        public class ItemStat
        {
            public required string Name { get; init; }
            public required string Value { get; init; }
        }

        public class ItemModifier
        {
            public required string Name { get; init; }
            public required string Value { get; init; }
        }
    }
}
