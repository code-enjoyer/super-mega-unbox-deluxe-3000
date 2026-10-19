using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SuperMegaUnboxDeluxe.Domain.Enums;

namespace SuperMegaUnboxDeluxe.Domain.Items.Generation;

public static class ItemCatalogValidator
{
    public static void Validate(IItemCatalog catalog)
    {
        Require(catalog.Bases.Length > 0 && catalog.Sources.Length > 0, "Bases and sources are required.");
        Distinct(catalog.Bases.Select(x => x.Id.ToString()), "base");
        Distinct(catalog.Affixes.Select(x => x.Id).Concat(catalog.Uniques.Select(x => x.Signature.Id)), "affix");
        Distinct(catalog.Uniques.Select(x => x.Id), "unique");
        Distinct(catalog.Sources.Select(x => x.Id), "source");
        Require(catalog.Sources.Any(x => x.Id == catalog.DefaultSourceId), "Default source is missing.");

        foreach (var itemBase in catalog.Bases)
        {
            Require(itemBase.Id != Guid.Empty && !string.IsNullOrWhiteSpace(itemBase.Name)
                && !string.IsNullOrWhiteSpace(itemBase.ImageKey), "Invalid base identity.");
            ValidateStats(itemBase.Stats);
        }

        foreach (var affix in catalog.Affixes.Concat(catalog.Uniques.Select(x => x.Signature)))
        {
            Require(!string.IsNullOrWhiteSpace(affix.Name) && Enum.IsDefined(affix.Slot), "Invalid affix.");
            Require((affix.Bonus is null) != (affix.Effect is null), "Affixes need exactly one bonus or effect.");
            Require(affix.CompatibleBases.Length > 0, "Affix has no compatible bases.");
            foreach (var baseId in affix.CompatibleBases)
            {
                var itemBase = catalog.Bases.SingleOrDefault(x => x.Id == baseId);
                Require(itemBase is not null, "Affix refers to an unknown base.");
                if (affix.Bonus is { } bonus)
                    Require(itemBase!.Stats.Any(x => x.Type == bonus.Type), "Bonus stat is missing from a compatible base.");
            }
            if (affix.Bonus is { } statBonus) ValidateRange(statBonus.Range);
            if (affix.Effect is { } effect)
            {
                Require(!string.IsNullOrWhiteSpace(effect.Id) && Enum.IsDefined(effect.Trigger), "Invalid effect identity.");
                ValidateRange(effect.ChancePercent, 100);
                if (effect.DurationSeconds is { } duration) ValidateRange(duration);
                if (effect.Count is { } count) ValidateRange(count);
                Require(!string.IsNullOrWhiteSpace(effect.DescriptionTemplate), "Effect description is missing.");
                try { _ = string.Format(CultureInfo.InvariantCulture, effect.DescriptionTemplate, 1, 2, 3); }
                catch (FormatException) { throw new InvalidOperationException("Invalid effect description format."); }
            }
        }
        Require(catalog.Affixes.All(x => x.Slot != AffixSlot.Signature), "Signature effects cannot be random affixes.");

        foreach (var unique in catalog.Uniques)
        {
            var itemBase = catalog.Bases.SingleOrDefault(x => x.Id == unique.BaseId);
            Require(itemBase is not null && !string.IsNullOrWhiteSpace(unique.Name)
                && !string.IsNullOrWhiteSpace(unique.ImageKey), "Invalid unique definition.");
            ValidateStats(unique.Stats);
            Require(unique.Stats.Select(x => x.Type).ToHashSet().SetEquals(itemBase!.Stats.Select(x => x.Type)),
                "Unique stats must match its base stat types.");
            Require(unique.Signature.Slot == AffixSlot.Signature && unique.Signature.Effect is not null
                && unique.Signature.CompatibleBases.Contains(unique.BaseId), "Invalid signature effect.");
            Require(unique.BonusAffixChancePercent is >= 0 and <= 100, "Invalid bonus affix chance.");
            if (unique.BonusAffixChancePercent > 0)
                Require(catalog.Affixes.Any(x => x.CompatibleBases.Contains(unique.BaseId)), "Unique bonus affix pool is empty.");
        }

        foreach (var source in catalog.Sources)
        {
            Require(!string.IsNullOrWhiteSpace(source.Name) && !string.IsNullOrWhiteSpace(source.Description), "Invalid source text.");
            Require(source.BaseIds.Length > 0 && source.BaseIds.Distinct().Count() == source.BaseIds.Length
                && source.BaseIds.All(id => catalog.Bases.Any(x => x.Id == id)), "Invalid source bases.");
            Require(source.UniqueIds.Distinct().Count() == source.UniqueIds.Length
                && source.UniqueIds.All(id => catalog.Uniques.Any(x => x.Id == id && source.BaseIds.Contains(x.BaseId))),
                "Invalid source unique pool.");
            Require(source.Rarities.Length > 0 && source.Rarities.Select(x => x.Rarity).Distinct().Count() == source.Rarities.Length
                && source.Rarities.All(x => Enum.IsDefined(x.Rarity) && x.Weight > 0)
                && source.Rarities.Sum(x => (long)x.Weight) <= int.MaxValue, "Invalid rarity weights.");
            if (source.Rarities.Any(x => x.Rarity == ItemRarity.Unique))
                Require(source.UniqueIds.Length > 0, "Unique rarity has no unique pool.");
            if (source.Rarities.Any(x => x.Rarity is not ItemRarity.Common and not ItemRarity.Unique))
                foreach (var baseId in source.BaseIds)
                    foreach (var slot in new[] { AffixSlot.Prefix, AffixSlot.Suffix })
                        Require(catalog.Affixes.Any(x => x.Slot == slot && x.CompatibleBases.Contains(baseId)),
                            "A source base has an empty affix slot pool.");
        }
    }

    private static void ValidateStats(IEnumerable<StatDefinition> stats)
    {
        var values = stats.ToArray();
        Require(values.Length > 0 && values.Select(x => x.Type).Distinct().Count() == values.Length, "Missing or duplicate stats.");
        foreach (var stat in values)
        {
            Require(Enum.IsDefined(stat.Type), "Unknown stat type.");
            ValidateRange(stat.Range);
        }
    }

    private static void ValidateRange(IntRange range, int max = 1_000_000) =>
        Require(range.Min >= 0 && range.Max >= range.Min && range.Max <= max, "Invalid roll range.");

    private static void Distinct(IEnumerable<string> ids, string kind)
    {
        var values = ids.ToArray();
        Require(values.All(x => !string.IsNullOrWhiteSpace(x)) && values.Distinct(StringComparer.Ordinal).Count() == values.Length,
            $"Empty or duplicate {kind} IDs.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"Invalid item catalog: {message}");
    }
}
