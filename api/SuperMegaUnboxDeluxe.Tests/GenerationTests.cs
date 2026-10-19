using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Domain.Enums;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using System.Collections.Immutable;

namespace SuperMegaUnboxDeluxe.Tests;

public sealed class GenerationTests
{
    private readonly StarterItemCatalog _catalog = new();

    [Theory]
    [InlineData(0, 1, ItemRarity.Common)]
    [InlineData(0, 55, ItemRarity.Common)]
    [InlineData(0, 56, ItemRarity.Uncommon)]
    [InlineData(0, 80, ItemRarity.Uncommon)]
    [InlineData(0, 81, ItemRarity.Rare)]
    [InlineData(0, 92, ItemRarity.Rare)]
    [InlineData(0, 93, ItemRarity.Epic)]
    [InlineData(0, 97, ItemRarity.Epic)]
    [InlineData(0, 98, ItemRarity.Legendary)]
    [InlineData(0, 99, ItemRarity.Legendary)]
    [InlineData(0, 100, ItemRarity.Unique)]
    [InlineData(1, 1, ItemRarity.Common)]
    [InlineData(1, 35, ItemRarity.Common)]
    [InlineData(1, 36, ItemRarity.Uncommon)]
    [InlineData(1, 65, ItemRarity.Uncommon)]
    [InlineData(1, 66, ItemRarity.Rare)]
    [InlineData(1, 85, ItemRarity.Rare)]
    [InlineData(1, 86, ItemRarity.Epic)]
    [InlineData(1, 95, ItemRarity.Epic)]
    [InlineData(1, 96, ItemRarity.Legendary)]
    [InlineData(1, 99, ItemRarity.Legendary)]
    [InlineData(1, 100, ItemRarity.Unique)]
    public void Rarity_boundaries_are_inclusive(int source, int draw, ItemRarity expected)
    {
        var result = new ItemGenerator(new SequenceRandom(draw)).Roll(_catalog, _catalog.Sources[source]);
        Assert.Equal(expected, result.Rarity);
    }

    [Theory]
    [InlineData(3, 40, 70)]
    [InlineData(8, 100, 180)]
    public void Common_stat_ranges_include_both_ends(int damage, int durability, int value)
    {
        var result = Roll(1, 0, damage, durability);
        Assert.Equal(damage.ToString(), result.Stats.Single(x => x.Type == ItemStatType.Damage).Value);
        Assert.Equal(durability.ToString(), result.Stats.Single(x => x.Type == ItemStatType.Durability).Value);
        Assert.Equal(value, result.Value);
        Assert.Empty(result.Modifiers);
        Assert.Equal("Finger Guns", result.Name);
    }

    [Fact]
    public void Rare_weapon_scales_then_adds_bonuses_and_snapshots_effect_parameters()
    {
        // rarity, base, stats, prefix/suffix selection, bonus, effect chance/duration
        var result = Roll(81, 0, 3, 40, 1, 0, 2, 15, 3);
        Assert.Equal("Aggressively Pointy Finger Guns of Existential Dread", result.Name);
        Assert.Equal("7", result.Stats.Single(x => x.Type == ItemStatType.Damage).Value);
        Assert.Equal("40", result.Stats.Single(x => x.Type == ItemStatType.Durability).Value);
        Assert.Equal(new StatBonus(ItemStatType.Damage, 2), result.Modifiers[0].Details!.Bonus);
        Assert.Equal(new SpecialEffect("existential-dread", EffectTrigger.OnHit, 15, 3, null), result.Modifiers[1].Details!.Effect);
        Assert.Contains("15%", result.Modifiers[1].Value);
        Assert.Contains("3 seconds", result.Modifiers[1].Value);
        Assert.Equal(203, result.Value); // (7*10 + 40 + 25)*1.5, midpoint away from zero
    }

    [Fact]
    public void Armour_gets_compatible_defense_and_durability_bonuses()
    {
        var result = Roll(81, 2, 5, 40, 0, 0, 5, 2);
        Assert.Equal("10", result.Stats.Single(x => x.Type == ItemStatType.Defense).Value);
        Assert.Equal("45", result.Stats.Single(x => x.Type == ItemStatType.Durability).Value);
        Assert.DoesNotContain(result.Stats, x => x.Type == ItemStatType.Damage);
        Assert.Equal("Suspiciously Moist Turtle-Shell Shield of Administrative Protection", result.Name);
    }

    [Fact]
    public void Geese_effect_preserves_its_trigger_and_count()
    {
        var result = Roll(81, 3, 2, 40, 0, 1, 5, 5, 1);
        Assert.Equal(new SpecialEffect("emergency-geese", EffectTrigger.OnBeingHit, 5, null, 1),
            result.Modifiers[1].Details!.Effect);
        Assert.Contains("1 advisory goose", result.Modifiers[1].Value);
    }

    [Theory]
    [InlineData(0, AffixSlot.Prefix)]
    [InlineData(1, AffixSlot.Suffix)]
    public void Uncommon_can_roll_either_slot(int slot, AffixSlot expected)
    {
        var result = Roll(56, 0, 3, 40, slot);
        Assert.Equal(expected, Assert.Single(result.Modifiers).Details!.Role);
    }

    [Theory]
    [InlineData(50, 2)]
    [InlineData(51, 1)]
    public void Unique_keeps_identity_and_signature_with_optional_bonus(int chance, int count)
    {
        var result = Roll(100, 0, 20, 80, chance);
        Assert.Equal(StarterItemCatalog.SalamanderId, result.BaseId);
        Assert.Equal("Sir Newt-on, the Unreasonably Rigid", result.Name);
        Assert.Equal("20", result.Stats.Single(x => x.Type == ItemStatType.Damage).Value);
        Assert.Equal(count, result.Modifiers.Length);
        Assert.Equal(AffixSlot.Signature, result.Modifiers[0].Details!.Role);
        Assert.Equal("challenge-gravity", result.Modifiers[0].Details!.Effect!.Id);
        Assert.Equal(count == 2 ? 1240 : 1220, result.Value);
    }

    [Fact]
    public void Each_source_rarity_and_base_has_valid_distinct_affixes()
    {
        foreach (var source in _catalog.Sources)
        foreach (var rarity in Enum.GetValues<ItemRarity>().Where(x => x != ItemRarity.Unique))
        foreach (var baseId in source.BaseIds)
        {
            var single = source with { BaseIds = [baseId], Rarities = [new(rarity, 1)] };
            var result = new ItemGenerator(new SequenceRandom()).Roll(_catalog, single);
            Assert.Equal(rarity == ItemRarity.Common ? 0 : rarity == ItemRarity.Uncommon ? 1 : 2, result.Modifiers.Length);
            Assert.Equal(result.Modifiers.Length, result.Modifiers.Select(x => x.Details!.DefinitionId).Distinct().Count());
            foreach (var modifier in result.Modifiers)
                Assert.Contains(baseId, _catalog.Affixes.Single(x => x.Id == modifier.Details!.DefinitionId).CompatibleBases);
        }
    }

    [Fact]
    public void Invalid_catalog_content_is_rejected()
    {
        var catalog = new TestCatalog(_catalog);
        Assert.Throws<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with { DefaultSourceId = "missing" }));
        Assert.Throws<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with { Bases = [catalog.Bases[0], catalog.Bases[0]] }));
        Assert.Throws<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with { Affixes = [] }));
        Assert.Throws<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with { Uniques = [] }));
        Assert.Throws<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with
        {
            Bases = catalog.Bases.SetItem(0, catalog.Bases[0] with { Stats = [new(ItemStatType.Damage, new(10, 1))] })
        }));
        Assert.Throws<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with
        {
            Sources = [catalog.Sources[0] with { Rarities = [new(ItemRarity.Common, 0)] }]
        }));
        Assert.Throws<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with
        {
            Affixes = catalog.Affixes.SetItem(0, catalog.Affixes[0] with { CompatibleBases = [Guid.NewGuid()] })
        }));
    }

    private ItemRoll Roll(params int[] values) => new ItemGenerator(new SequenceRandom(values)).Roll(_catalog, _catalog.Sources[0]);

    private sealed record TestCatalog : IItemCatalog
    {
        public string DefaultSourceId { get; init; }
        public ImmutableArray<BaseDefinition> Bases { get; init; }
        public ImmutableArray<AffixDefinition> Affixes { get; init; }
        public ImmutableArray<UniqueDefinition> Uniques { get; init; }
        public ImmutableArray<LootSourceDefinition> Sources { get; init; }

        public TestCatalog(IItemCatalog original)
        {
            DefaultSourceId = original.DefaultSourceId;
            Bases = original.Bases;
            Affixes = original.Affixes;
            Uniques = original.Uniques;
            Sources = original.Sources;
        }
    }
}

internal sealed class SequenceRandom(params int[] values) : IRandomSource
{
    private readonly Queue<int> _values = new(values);
    public int NextInclusive(int min, int max)
    {
        var value = _values.TryDequeue(out var next) ? next : min;
        Assert.InRange(value, min, max);
        return value;
    }
}
