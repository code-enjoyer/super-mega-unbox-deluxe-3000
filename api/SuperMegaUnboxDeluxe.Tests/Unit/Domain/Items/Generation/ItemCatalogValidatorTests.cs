using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Domain.Enums;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using System.Collections.Immutable;

namespace SuperMegaUnboxDeluxe.Tests.Unit.Domain.Items.Generation;

[Trait("Category", "Unit")]
public sealed class ItemCatalogValidatorTests
{
    [Fact]
    public void ItemCatalogValidator_InvalidCatalogContent_ShouldThrow()
    {
        var catalog = new TestCatalog(new StarterItemCatalog());
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
