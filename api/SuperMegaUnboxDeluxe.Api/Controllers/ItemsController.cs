using Microsoft.AspNetCore.Mvc;
using SuperMegaUnboxDeluxe.Application.Items;
using SuperMegaUnboxDeluxe.Application.Repositories;
using SuperMegaUnboxDeluxe.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperMegaUnboxDeluxe.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IItemRepository _itemRepository;
    private readonly GenerateItemService _generateItem;

    public ItemsController(IItemRepository itemRepository, GenerateItemService generateItem)
    {
        _itemRepository = itemRepository;
        _generateItem = generateItem;
    }

    [HttpGet("{id:guid}", Name = Constants.Actions.GetItemDetails)]
    public async Task<ActionResult<Contracts.Item>> GetItemDetailsAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(id, cancellationToken);

        return item is null
            ? NotFound()
            : Ok(ToContract(item));
    }

    [HttpPost(Name = Constants.Actions.GenerateItem)]
    public async Task<ActionResult<Contracts.Item>> GenerateItemAsync([FromBody] Contracts.GenerateItemRequest? request, CancellationToken cancellationToken)
    {
        var item = await _generateItem.GenerateAsync(request?.LootSourceId, cancellationToken);

        if (item is null)
            return Problem(statusCode: 400, title: "Unknown loot source",
                detail: "Choose a lootSourceId returned by GET /loot-sources.");

        return CreatedAtAction(Constants.Actions.GetItemDetails,
            new { id = item.Id },
            ToContract(item));
    }

    private static Contracts.Item ToContract(Item item) => new()
    {
        Id = item.Id,
        ItemBase = item.Base.Name,
        Name = item.Name,
        Stats = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(item.Stats, stat => new Contracts.Item.ItemStat { Name = stat.Type.ToString(), Value = stat.Value })),
        Modifiers = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(item.Modifiers, modifier => new Contracts.Item.ItemModifier
        {
            Name = modifier.Name,
            Value = modifier.Value,
            Details = modifier.Details is { } details ? new Contracts.Item.ModifierDetails(
                details.DefinitionId, details.Role.ToString(),
                details.Bonus is { } bonus ? new Contracts.Item.StatBonus(bonus.Type.ToString(), bonus.Amount) : null,
                details.Effect is { } effect ? new Contracts.Item.SpecialEffect(effect.Id, effect.Trigger.ToString(),
                    effect.ChancePercent, effect.DurationSeconds, effect.Count) : null) : null
        })),
        Rarity = item.Rarity.ToString(),
        ImageKey = item.ImageKey,
        DateGotten = item.DateGotten,
        Value = item.Value
    };
}
