using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Application.Items;
using SuperMegaUnboxDeluxe.Application.Items.Abstractions;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Application.Items.GenerateItem;
using SuperMegaUnboxDeluxe.Application.Items.GetItemDetails;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using SuperMegaUnboxDeluxe.Tests.TestSupport;

namespace SuperMegaUnboxDeluxe.Tests.Unit.Application.Items;

[Trait("Category", "Unit")]
public sealed class GenerateItemHandlerTests
{
    private readonly IItemBaseRepository _bases = Substitute.For<IItemBaseRepository>();
    private readonly IItemRepository _items = Substitute.For<IItemRepository>();
    private readonly IUnitOfWork _unit = Substitute.For<IUnitOfWork>();

    public GenerateItemHandlerTests()
    {
        _bases.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => new ItemBase(call.Arg<Guid>(), "Finger Guns"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("suspicious-lost-property")]
    [InlineData("definitely-legal-arms-dealer")]
    public async Task GenerateItemHandler_KnownOrDefaultSource_ShouldSaveOnceWithSuppliedTime(string? source)
    {
        using var cancellation = new CancellationTokenSource();
        var result = await Handler().HandleAsync(new GenerateItemCommand(source), cancellation.Token);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DateGotten.ShouldBe(FixedTime.Now);
        _items.Received(1).Add(Arg.Is<Item>(item => item.Id == result.Value.Id && item.Base.Id == StarterItemCatalog.FingerGunsId));
        await _bases.Received(1).GetByIdAsync(StarterItemCatalog.FingerGunsId, cancellation.Token);
        await _unit.Received(1).SaveChangesAsync(cancellation.Token);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GenerateItemHandler_UnknownSource_ShouldNotReadOrWrite(string source)
    {
        var result = await Handler().HandleAsync(new GenerateItemCommand(source), default);
        result.Error.ShouldBe(ItemErrors.UnknownSource);
        await _bases.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        _items.DidNotReceiveWithAnyArgs().Add(default!);
        await _unit.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task GenerateItemHandler_CancelledBeforeGeneration_ShouldNotReadOrWrite()
    {
        await Should.ThrowAsync<OperationCanceledException>(() => Handler().HandleAsync(new GenerateItemCommand(null), new CancellationToken(true)));
        await _bases.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        _items.DidNotReceiveWithAnyArgs().Add(default!);
        await _unit.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task GenerateItemHandler_CancelledAfterBaseLookup_ShouldNotAddItem()
    {
        using var cancellation = new CancellationTokenSource();
        _bases.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            cancellation.Cancel();
            return new ItemBase(call.Arg<Guid>(), "Finger Guns");
        });
        await Should.ThrowAsync<OperationCanceledException>(() => Handler().HandleAsync(new GenerateItemCommand(null), cancellation.Token));
        _items.DidNotReceiveWithAnyArgs().Add(default!);
        await _unit.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task GenerateItemHandler_MissingSeedData_ShouldThrowWithoutInsertingReplacementBase()
    {
        _bases.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ItemBase?)null);
        await Should.ThrowAsync<InvalidOperationException>(() => Handler().HandleAsync(new GenerateItemCommand(null), default));
        _items.DidNotReceiveWithAnyArgs().Add(default!);
        await _unit.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task GenerateItemHandler_SaveFailure_ShouldPropagateException()
    {
        _unit.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(new InvalidOperationException("Test save failure.")));
        await Should.ThrowAsync<InvalidOperationException>(() => Handler().HandleAsync(new GenerateItemCommand(null), default));
        await _unit.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task GetItemDetailsHandler_MissingItem_ShouldReturnNotFoundAndForwardCancellation()
    {
        var queries = Substitute.For<IItemQueries>();
        var id = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var result = await new GetItemDetailsHandler(queries).HandleAsync(new GetItemDetailsQuery(id), cancellation.Token);
        result.Error.ShouldBe(ItemErrors.NotFound);
        await queries.Received(1).GetByIdAsync(id, cancellation.Token);
    }

    private GenerateItemHandler Handler()
    {
        var catalog = new StarterItemCatalog();
        return new GenerateItemHandler(catalog, new ItemGenerator(new SequenceRandom()), _bases, _items, _unit,
            new FixedTime(), new GenerateItemValidator(catalog));
    }

    private sealed class FixedTime : TimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 9, 24, 1, 2, 3, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now.AddTicks(7);
    }
}
