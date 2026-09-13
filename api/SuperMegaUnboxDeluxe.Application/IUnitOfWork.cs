using System.Threading;
using System.Threading.Tasks;

namespace SuperMegaUnboxDeluxe.Application;

public interface IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
