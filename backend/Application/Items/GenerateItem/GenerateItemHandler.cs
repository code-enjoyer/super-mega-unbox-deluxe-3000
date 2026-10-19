using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using SuperMegaUnboxDeluxe.Application.Items.Abstractions;
using SuperMegaUnboxDeluxe.Common.Results;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

namespace SuperMegaUnboxDeluxe.Application.Items.GenerateItem;

public sealed class GenerateItemHandler
{
    private readonly IItemCatalog _catalog;
    private readonly IValidator<GenerateItemCommand> _validator;
    private readonly ItemGenerator _generator;
    private readonly IItemBaseRepository _bases;
    private readonly IItemRepository _items;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public GenerateItemHandler(IItemCatalog catalog, ItemGenerator generator, IItemBaseRepository bases,
        IItemRepository items, IUnitOfWork unitOfWork, TimeProvider time, IValidator<GenerateItemCommand> validator)
    {
        _catalog = catalog;
        _validator = validator;
        _generator = generator;
        _bases = bases;
        _items = items;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<Result<ItemDetails>> HandleAsync(GenerateItemCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return Result<ItemDetails>.Failure(ItemErrors.UnknownSource);
        var source = _catalog.Sources.Single(x => x.Id == (request.LootSourceId ?? _catalog.DefaultSourceId));

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
        return Result<ItemDetails>.Success(ItemDetails.FromItem(item));
    }
}
