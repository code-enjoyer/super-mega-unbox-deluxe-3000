using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

namespace SuperMegaUnboxDeluxe.Application.Items.GetLootSources;

public sealed class GetLootSourcesHandler(IItemCatalog catalog)
{
    public Task<LootSourceDetails[]> HandleAsync(GetLootSourcesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(catalog.Sources.Select(source => new LootSourceDetails(source.Id, source.Name, source.Description)).ToArray());
    }
}
