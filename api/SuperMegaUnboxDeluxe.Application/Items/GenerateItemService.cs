using SuperMegaUnboxDeluxe.Application.Repositories;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SuperMegaUnboxDeluxe.Application.Items;

public sealed class GenerateItemService
{
    private readonly IItemCatalog _catalog;
    private readonly ItemGenerator _generator;
    private readonly IItemBaseRepository _bases;
    private readonly IItemRepository _items;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public GenerateItemService(IItemCatalog catalog, ItemGenerator generator, IItemBaseRepository bases,
        IItemRepository items, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _catalog = catalog;
        _generator = generator;
        _bases = bases;
        _items = items;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    // A null result means an unknown source. No item is added or saved in that case.
    public async Task<Item?> GenerateAsync(string? lootSourceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source = _catalog.Sources.SingleOrDefault(x => x.Id == (lootSourceId ?? _catalog.DefaultSourceId));
        if (source is null) return null;

        var roll = _generator.Roll(_catalog, source);
        var itemBase = await _bases.GetByIdAsync(roll.BaseId, cancellationToken)
            ?? throw new InvalidOperationException("The item catalog base is missing from the database. Apply pending migrations.");
        cancellationToken.ThrowIfCancellationRequested();
        // PostgreSQL timestamps preserve microseconds. Use the same precision in the
        // creation response so a later read returns the identical acquisition time.
        var acquiredAt = _time.GetUtcNow();
        acquiredAt = acquiredAt.AddTicks(-(acquiredAt.Ticks % TimeSpan.TicksPerMicrosecond));
        var item = roll.CreateItem(itemBase, acquiredAt);
        _items.Add(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return item;
    }
}
