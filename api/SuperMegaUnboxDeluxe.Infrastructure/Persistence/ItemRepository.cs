using Microsoft.EntityFrameworkCore;
using SuperMegaUnboxDeluxe.Application.Repositories;
using SuperMegaUnboxDeluxe.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence;

internal sealed class ItemRepository : IItemRepository
{
    private readonly SmudDbContext _dbContext;

    public ItemRepository(SmudDbContext dbContext) => _dbContext = dbContext;

    public void Add(Item item) => _dbContext.Items.Add(item);

    public Task<Item?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // Aggregate repository reads stay tracked so future domain changes can be
        // committed through IUnitOfWork. TODO: Add a separate projected read/query
        // path for read-only item details if tracking becomes unnecessary overhead.
        return _dbContext.Items
            .Include(x => x.Base)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
