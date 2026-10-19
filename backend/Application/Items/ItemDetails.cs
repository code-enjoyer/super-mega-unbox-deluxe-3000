using System;
using System.Linq;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

namespace SuperMegaUnboxDeluxe.Application.Items;

public sealed record ItemDetails(
    Guid Id, string ItemBase, string Name, ItemStatDetails[] Stats, ItemModifierDetails[] Modifiers,
    string Rarity, string ImageKey, DateTimeOffset DateGotten, float Value)
{
    public static ItemDetails FromItem(Item item) => new(
        item.Id, item.Base.Name, item.Name,
        item.Stats.Select(stat => new ItemStatDetails(stat.Type.ToString(), stat.Value)).ToArray(),
        item.Modifiers.Select(modifier => new ItemModifierDetails(modifier.Name, modifier.Value, MapDetails(modifier.Details))).ToArray(),
        item.Rarity.ToString(), item.ImageKey, item.DateGotten, item.Value);

    public static ModifierSnapshot? MapDetails(ModifierDetails? details) => details is null ? null : new(
        details.DefinitionId, details.Role.ToString(),
        details.Bonus is { } bonus ? new StatBonusDetails(bonus.Type.ToString(), bonus.Amount) : null,
        details.Effect is { } effect ? new SpecialEffectDetails(effect.Id, effect.Trigger.ToString(), effect.ChancePercent, effect.DurationSeconds, effect.Count) : null);
}

public sealed record ItemStatDetails(string Name, string Value);
public sealed record ItemModifierDetails(string Name, string Value, ModifierSnapshot? Details);
public sealed record ModifierSnapshot(string DefinitionId, string Role, StatBonusDetails? Bonus, SpecialEffectDetails? Effect);
public sealed record StatBonusDetails(string Type, int Amount);
public sealed record SpecialEffectDetails(string Id, string Trigger, int ChancePercent, int? DurationSeconds, int? Count);
