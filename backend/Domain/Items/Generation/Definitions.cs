using System;
using System.Collections.Immutable;
using SuperMegaUnboxDeluxe.Domain.Enums;

namespace SuperMegaUnboxDeluxe.Domain.Items.Generation;

public enum AffixSlot { Prefix, Suffix, Signature }
public enum EffectTrigger { OnHit, OnBeingHit }

public sealed record IntRange(int Min, int Max);
public sealed record StatDefinition(ItemStatType Type, IntRange Range);
public sealed record BaseDefinition(Guid Id, string Name, string ImageKey, ImmutableArray<StatDefinition> Stats);
public sealed record StatBonusDefinition(ItemStatType Type, IntRange Range);
public sealed record EffectDefinition(
    string Id, EffectTrigger Trigger, IntRange ChancePercent,
    IntRange? DurationSeconds, IntRange? Count, string DescriptionTemplate);
public sealed record AffixDefinition(
    string Id, string Name, AffixSlot Slot, ImmutableArray<Guid> CompatibleBases,
    StatBonusDefinition? Bonus, EffectDefinition? Effect);
public sealed record UniqueDefinition(
    string Id, Guid BaseId, string Name, string ImageKey, ImmutableArray<StatDefinition> Stats,
    AffixDefinition Signature, int BonusAffixChancePercent);
public sealed record RarityWeight(ItemRarity Rarity, int Weight);
public sealed record LootSourceDefinition(
    string Id, string Name, string Description, ImmutableArray<Guid> BaseIds,
    ImmutableArray<string> UniqueIds, ImmutableArray<RarityWeight> Rarities);

public interface IItemCatalog
{
    string DefaultSourceId { get; }
    ImmutableArray<BaseDefinition> Bases { get; }
    ImmutableArray<AffixDefinition> Affixes { get; }
    ImmutableArray<UniqueDefinition> Uniques { get; }
    ImmutableArray<LootSourceDefinition> Sources { get; }
}

public interface IRandomSource
{
    // Both bounds are inclusive, including for single-value ranges.
    int NextInclusive(int min, int max);
}

// Immutable snapshots of the rolled contribution, independent of future catalog changes.
public sealed record StatBonus(ItemStatType Type, int Amount);
public sealed record SpecialEffect(
    string Id, EffectTrigger Trigger, int ChancePercent, int? DurationSeconds, int? Count);
public sealed record ModifierDetails(
    string DefinitionId, AffixSlot Role, StatBonus? Bonus, SpecialEffect? Effect);
