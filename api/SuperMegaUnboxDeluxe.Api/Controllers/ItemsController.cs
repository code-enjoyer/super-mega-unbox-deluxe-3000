using Microsoft.AspNetCore.Mvc;
using SuperMegaUnboxDeluxe.Api.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperMegaUnboxDeluxe.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ItemsController : ControllerBase
{
    [HttpGet("{id:guid}", Name = Constants.Actions.GetItemDetails)]
    public async Task<ActionResult<Item>> GetItemDetailsAsync([FromRoute] Guid id, CancellationToken cancellationToken)
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

    [HttpPost(Name = Constants.Actions.GenerateItem)]
    public async Task<ActionResult<Item>> GenerateItemAsync([FromBody] GenerateItemRequest? request, CancellationToken cancellationToken)
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

        return CreatedAtAction(Constants.Actions.GetItemDetails,
            new { id = item.Id },
            item);
    }
}