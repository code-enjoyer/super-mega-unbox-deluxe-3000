namespace SuperMegaUnboxDeluxe.Application.Repositories;

public interface IItemRepository
{
    void Add(Domain.Entities.Item item);
    System.Threading.Tasks.Task<Domain.Entities.Item?> GetByIdAsync(
        System.Guid id,
        System.Threading.CancellationToken cancellationToken);
}
