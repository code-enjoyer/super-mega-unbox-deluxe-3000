using System.Linq;
using SuperMegaUnboxDeluxe.Application.Items;

namespace SuperMegaUnboxDeluxe.Api.Contracts;

public static class ItemContractMapper
{
    public static Item ToContract(this ItemDetails item) => new()
    {
        Id = item.Id,
        ItemBase = item.ItemBase,
        Name = item.Name,
        Rarity = item.Rarity,
        ImageKey = item.ImageKey,
        DateGotten = item.DateGotten,
        Value = item.Value,
        Stats = item.Stats.Select(stat => new Item.ItemStat { Name = stat.Name, Value = stat.Value }).ToArray(),
        Modifiers = item.Modifiers.Select(modifier => new Item.ItemModifier
        {
            Name = modifier.Name,
            Value = modifier.Value,
            Details = modifier.Details is { } details ? new Item.ModifierDetails(details.DefinitionId, details.Role,
                details.Bonus is { } bonus ? new Item.StatBonus(bonus.Type, bonus.Amount) : null,
                details.Effect is { } effect ? new Item.SpecialEffect(effect.Id, effect.Trigger, effect.ChancePercent, effect.DurationSeconds, effect.Count) : null) : null
        }).ToArray()
    };
}
