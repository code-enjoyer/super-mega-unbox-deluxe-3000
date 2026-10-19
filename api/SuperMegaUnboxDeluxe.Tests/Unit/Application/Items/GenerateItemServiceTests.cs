using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Application.Items;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Application.Repositories;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using SuperMegaUnboxDeluxe.Tests.TestSupport;

namespace SuperMegaUnboxDeluxe.Tests.Unit.Application.Items;

[Trait("Category", "Unit")]
public sealed class GenerateItemServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("suspicious-lost-property")]
    [InlineData("definitely-legal-arms-dealer")]
    public async Task GenerateItemService_KnownOrDefaultSource_ShouldSaveOnceWithSuppliedTime(string? source)
    {
        var store = new FakeStore();
        using var cancellation = new CancellationTokenSource();
        var item = await Service(store).GenerateAsync(source, cancellation.Token);
        Assert.NotNull(item);
        Assert.Equal(FixedTime.Now, item.DateGotten);
        Assert.Equal(StarterItemCatalog.FingerGunsId, item.Base.Id);
        Assert.Same(item, Assert.Single(store.Items));
        Assert.Equal(1, store.Saves);
        Assert.Equal(cancellation.Token, store.ReadToken);
        Assert.Equal(cancellation.Token, store.SaveToken);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GenerateItemService_UnknownSource_ShouldNotReadOrWrite(string source)
    {
        var store = new FakeStore();
        Assert.Null(await Service(store).GenerateAsync(source, default));
        Assert.Equal(0, store.Reads);
        Assert.Equal(0, store.Saves);
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task GenerateItemService_CancelledBeforeGeneration_ShouldNotReadOrWrite()
    {
        var store = new FakeStore();
        await Assert.ThrowsAsync<OperationCanceledException>(() => Service(store).GenerateAsync(null, new CancellationToken(true)));
        Assert.Equal(0, store.Reads);
        Assert.Empty(store.Items);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task GenerateItemService_CancelledAfterBaseLookup_ShouldNotAddItem()
    {
        using var cancellation = new CancellationTokenSource();
        var store = new FakeStore { AfterRead = cancellation.Cancel };
        await Assert.ThrowsAsync<OperationCanceledException>(() => Service(store).GenerateAsync(null, cancellation.Token));
        Assert.Empty(store.Items);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task GenerateItemService_MissingSeedData_ShouldThrowWithoutInsertingReplacementBase()
    {
        var store = new FakeStore { MissingBase = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(store).GenerateAsync(null, default));
        Assert.Empty(store.Items);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task GenerateItemService_SaveFailure_ShouldPropagateException()
    {
        var store = new FakeStore { FailSave = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(store).GenerateAsync(null, default));
        Assert.Equal(1, store.Saves);
    }

    private static GenerateItemService Service(FakeStore store) => new(new StarterItemCatalog(),
        new ItemGenerator(new SequenceRandom()), store, store, store, new FixedTime());

    private sealed class FixedTime : TimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 9, 24, 1, 2, 3, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now.AddTicks(7);
    }

    private sealed class FakeStore : IItemBaseRepository, IItemRepository, IUnitOfWork
    {
        public List<Item> Items { get; } = [];
        public int Reads { get; private set; }
        public int Saves { get; private set; }
        public CancellationToken ReadToken { get; private set; }
        public CancellationToken SaveToken { get; private set; }
        public bool MissingBase { get; init; }
        public bool FailSave { get; init; }
        public Action? AfterRead { get; init; }

        public void Add(Item item) => Items.Add(item);
        Task<Item?> IItemRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

        public Task<ItemBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Reads++;
            ReadToken = cancellationToken;
            AfterRead?.Invoke();
            return Task.FromResult(MissingBase ? null : new ItemBase(id, "Finger Guns"));
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saves++;
            SaveToken = cancellationToken;
            if (FailSave) throw new InvalidOperationException("Test save failure.");
            return Task.FromResult(1);
        }
    }
}
