using SuperMegaUnboxDeluxe.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperMegaUnboxDeluxe.Application.Repositories;

public interface IItemBaseRepository
{
    Task<ItemBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
