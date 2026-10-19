using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace SuperMegaUnboxDeluxe.Domain.Items.Generation;

public sealed record ItemRoll(
    Guid BaseId, string Name, ItemRarity Rarity, string ImageKey, int Value,
    ImmutableArray<ItemStat> Stats, ImmutableArray<ItemModifier> Modifiers)
{
    public Item CreateItem(ItemBase itemBase, DateTimeOffset acquiredAt)
    {
        if (itemBase.Id != BaseId)
            throw new ArgumentException("The persisted base does not match the rolled base.", nameof(itemBase));

        return new Item(itemBase, Name, Rarity, ImageKey, acquiredAt, Value, Stats, Modifiers);
    }
}

public sealed class ItemGenerator
{
    private readonly IRandomSource _random;

    public ItemGenerator(IRandomSource random) => _random = random;

    public ItemRoll Roll(IItemCatalog catalog, LootSourceDefinition source)
    {
        var rarity = RollRarity(source.Rarities);
        var uniqueId = rarity == ItemRarity.Unique ? Pick(source.UniqueIds) : null;
        var unique = uniqueId is null ? null : catalog.Uniques.Single(x => x.Id == uniqueId);
        var baseId = unique?.BaseId ?? Pick(source.BaseIds);
        var definition = catalog.Bases.Single(x => x.Id == baseId);
        var multiplier = Multiplier(rarity);
        var stats = (unique?.Stats ?? definition.Stats).ToDictionary(
            x => x.Type,
            x => Scale(Roll(x.Range), unique is null && x.Type != ItemStatType.Durability ? multiplier : 1m));
        var selected = new List<AffixDefinition>();
        var compatible = catalog.Affixes.Where(x => x.CompatibleBases.Contains(baseId)).ToArray();

        if (unique is not null)
        {
            selected.Add(unique.Signature);
            if (_random.NextInclusive(1, 100) <= unique.BonusAffixChancePercent)
                selected.Add(Pick(compatible));
        }
        else if (rarity == ItemRarity.Uncommon)
        {
            var slot = _random.NextInclusive(0, 1) == 0 ? AffixSlot.Prefix : AffixSlot.Suffix;
            selected.Add(Pick(compatible.Where(x => x.Slot == slot).ToArray()));
        }
        else if (rarity != ItemRarity.Common)
        {
            selected.Add(Pick(compatible.Where(x => x.Slot == AffixSlot.Prefix).ToArray()));
            selected.Add(Pick(compatible.Where(x => x.Slot == AffixSlot.Suffix).ToArray()));
        }

        var modifiers = selected.Select(affix => RollModifier(affix, stats)).ToImmutableArray();
        var name = unique?.Name ?? string.Join(" ", new[]
        {
            selected.FirstOrDefault(x => x.Slot == AffixSlot.Prefix)?.Name,
            definition.Name,
            selected.FirstOrDefault(x => x.Slot == AffixSlot.Suffix)?.Name
        }.Where(x => !string.IsNullOrEmpty(x)));
        var value = stats.Sum(x => x.Value * (x.Key == ItemStatType.Durability ? 1 : 10))
            + modifiers.Count(x => x.Details?.Effect is not null) * 25;

        return new ItemRoll(baseId, name, rarity, unique?.ImageKey ?? definition.ImageKey,
            Scale(value, multiplier),
            stats.Select(x => new ItemStat(x.Key, x.Value.ToString(CultureInfo.InvariantCulture))).ToImmutableArray(),
            modifiers);
    }

    private ItemModifier RollModifier(AffixDefinition definition, Dictionary<ItemStatType, int> stats)
    {
        StatBonus? bonus = null;
        SpecialEffect? effect = null;
        string description;
        if (definition.Bonus is { } bonusDefinition)
        {
            bonus = new StatBonus(bonusDefinition.Type, Roll(bonusDefinition.Range));
            stats[bonus.Type] += bonus.Amount;
            description = $"+{bonus.Amount.ToString(CultureInfo.InvariantCulture)} {bonus.Type}";
        }
        else
        {
            var effectDefinition = definition.Effect!;
            effect = new SpecialEffect(effectDefinition.Id, effectDefinition.Trigger,
                Roll(effectDefinition.ChancePercent),
                effectDefinition.DurationSeconds is { } duration ? Roll(duration) : null,
                effectDefinition.Count is { } count ? Roll(count) : null);
            description = string.Format(CultureInfo.InvariantCulture, effectDefinition.DescriptionTemplate,
                effect.ChancePercent, effect.DurationSeconds, effect.Count);
        }

        return new ItemModifier(definition.Name, description,
            new ModifierDetails(definition.Id, definition.Slot, bonus, effect));
    }

    private ItemRarity RollRarity(ImmutableArray<RarityWeight> weights)
    {
        var roll = _random.NextInclusive(1, weights.Sum(x => x.Weight));
        foreach (var entry in weights)
        {
            roll -= entry.Weight;
            if (roll <= 0) return entry.Rarity;
        }
        throw new InvalidOperationException("Loot source has no rarity weights.");
    }

    private T Pick<T>(IReadOnlyList<T> values) => values[_random.NextInclusive(0, values.Count - 1)];
    private int Roll(IntRange range) => _random.NextInclusive(range.Min, range.Max);
    private static int Scale(int value, decimal multiplier) =>
        checked((int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero));

    private static decimal Multiplier(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => 1m,
        ItemRarity.Uncommon => 1.2m,
        ItemRarity.Rare => 1.5m,
        ItemRarity.Epic => 2m,
        ItemRarity.Legendary => 3m,
        ItemRarity.Unique => 4m,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity))
    };
}
