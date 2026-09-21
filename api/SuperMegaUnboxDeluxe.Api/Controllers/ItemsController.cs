using Microsoft.AspNetCore.Mvc;
using SuperMegaUnboxDeluxe.Api.Contracts;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Application.Repositories;
using SuperMegaUnboxDeluxe.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using DomainItem = SuperMegaUnboxDeluxe.Domain.Entities.Item;
using DomainItemBase = SuperMegaUnboxDeluxe.Domain.Entities.ItemBase;
using DomainItemModifier = SuperMegaUnboxDeluxe.Domain.Entities.ItemModifier;
using DomainItemStat = SuperMegaUnboxDeluxe.Domain.Entities.ItemStat;

namespace SuperMegaUnboxDeluxe.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ItemsController(IItemRepository itemRepository, IUnitOfWork unitOfWork)
    {
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("{id:guid}", Name = Constants.Actions.GetItemDetails)]
    public async Task<ActionResult<Item>> GetItemDetailsAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(id, cancellationToken);

        return item is null ? NotFound() : Ok(ToContract(item));
    }

    [HttpPost(Name = Constants.Actions.GenerateItem)]
    public async Task<ActionResult<Item>> GenerateItemAsync([FromBody] GenerateItemRequest? request, CancellationToken cancellationToken)
    {
        var item = new DomainItem(
            new DomainItemBase(request?.ItemType ?? "Sword"),
            "Excalibur",
            ItemRarity.Legendary,
            "excalibur",
            DateTimeOffset.UtcNow,
            1000.0f,
            new[] { new DomainItemStat(ItemStatType.Attack, "100"), new DomainItemStat(ItemStatType.Durability, "80") },
            new[] { new DomainItemModifier("Critical Hit Chance", "+10%"), new DomainItemModifier("Attack Speed", "+5%") });

        _itemRepository.Add(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        Modifiers = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(item.Modifiers, modifier => new Item.ItemModifier { Name = modifier.Name, Value = modifier.Value })),
        Rarity = item.Rarity.ToString(),
        ImageKey = item.ImageKey,
        DateGotten = item.DateGotten,
        Value = item.Value
    };
}
