using Microsoft.AspNetCore.Mvc;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Application.Repositories;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Contracts = SuperMegaUnboxDeluxe.Api.Contracts;

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
    public async Task<ActionResult<Contracts.Item>> GetItemDetailsAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(id, cancellationToken);

        return item is null ? NotFound() : Ok(ToContract(item));
    }

    [HttpPost(Name = Constants.Actions.GenerateItem)]
    public async Task<ActionResult<Contracts.Item>> GenerateItemAsync([FromBody] Contracts.GenerateItemRequest? request, CancellationToken cancellationToken)
    {
        var item = new Item(
            itemBase: new ItemBase(name: request?.ItemType ?? "Sword"),
            name: "Excalibur",
            rarity: ItemRarity.Legendary,
            imageKey: "excalibur",
            dateGotten: DateTimeOffset.UtcNow,
            value: 1000.0f,
            stats: new[] { new ItemStat(type: ItemStatType.Attack, value: "100"), new ItemStat(type: ItemStatType.Durability, value: "80") },
            modifiers: new[] { new ItemModifier(name: "Critical Hit Chance", value: "+10%"), new ItemModifier(name: "Attack Speed", value: "+5%") });

        _itemRepository.Add(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        Modifiers = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(item.Modifiers, modifier => new Contracts.Item.ItemModifier { Name = modifier.Name, Value = modifier.Value })),
        Rarity = item.Rarity.ToString(),
        ImageKey = item.ImageKey,
        DateGotten = item.DateGotten,
        Value = item.Value
    };
}
