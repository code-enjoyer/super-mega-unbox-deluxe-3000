using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SuperMegaUnboxDeluxe.Application.Items.Abstractions;
using SuperMegaUnboxDeluxe.Domain.Entities;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence;

internal sealed class ItemBaseRepository : IItemBaseRepository
{
    private readonly SmudDbContext _dbContext;

    public ItemBaseRepository(SmudDbContext dbContext) => _dbContext = dbContext;

    public Task<ItemBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<ItemBase>().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}
