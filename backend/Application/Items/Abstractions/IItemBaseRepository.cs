using System;
using System.Threading;
using System.Threading.Tasks;
using SuperMegaUnboxDeluxe.Domain.Entities;

namespace SuperMegaUnboxDeluxe.Application.Items.Abstractions;

public interface IItemBaseRepository
{
    Task<ItemBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
