using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperMegaUnboxDeluxe.Application.Items.Abstractions;

public interface IItemQueries
{
    Task<ItemDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
