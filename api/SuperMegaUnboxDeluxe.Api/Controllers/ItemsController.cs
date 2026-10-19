using Microsoft.AspNetCore.Mvc;
using SuperMegaUnboxDeluxe.Api.Contracts;
using SuperMegaUnboxDeluxe.Application.Items;
using SuperMegaUnboxDeluxe.Application.Repositories;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading;
using System.Threading.Tasks;
using DomainItem = SuperMegaUnboxDeluxe.Domain.Entities.Item;

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
    public async Task<ActionResult<Item>> GetItemDetailsAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(id, cancellationToken);

        return item is null ? NotFound() : Ok(ToContract(item));
    }

    [HttpPost(Name = Constants.Actions.GenerateItem)]
    public async Task<ActionResult<Item>> GenerateItemAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GenerateItemRequest? request,
        CancellationToken cancellationToken)
    {
        var item = await _generateItem.GenerateAsync(request?.LootSourceId, cancellationToken);
        if (item is null)
            return Problem(statusCode: 400, title: "Unknown loot source",
                detail: "Choose a lootSourceId returned by GET /loot-sources.");

        return CreatedAtAction(Constants.Actions.GetItemDetails,
            new { id = item.Id },
            ToContract(item));
    }

    private static Item ToContract(DomainItem item) => new()
    {
        Id = item.Id,
        ItemBase = item.Base.Name,
        Name = item.Name,
        Stats = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(item.Stats, stat => new Item.ItemStat { Name = stat.Type.ToString(), Value = stat.Value })),
        Modifiers = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(item.Modifiers, modifier => new Item.ItemModifier
        {
            Name = modifier.Name,
            Value = modifier.Value,
            Details = modifier.Details is { } details ? new Item.ModifierDetails(
                details.DefinitionId, details.Role.ToString(),
                details.Bonus is { } bonus ? new Item.StatBonus(bonus.Type.ToString(), bonus.Amount) : null,
                details.Effect is { } effect ? new Item.SpecialEffect(effect.Id, effect.Trigger.ToString(),
                    effect.ChancePercent, effect.DurationSeconds, effect.Count) : null) : null
        })),
        Rarity = item.Rarity.ToString(),
        ImageKey = item.ImageKey,
        DateGotten = item.DateGotten,
        Value = item.Value
    };
}
