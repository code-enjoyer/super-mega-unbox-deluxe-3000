using System.Threading;
using System.Threading.Tasks;
using SuperMegaUnboxDeluxe.Application.Items.Abstractions;
using SuperMegaUnboxDeluxe.Common.Results;

namespace SuperMegaUnboxDeluxe.Application.Items.GetItemDetails;

public sealed class GetItemDetailsHandler(IItemQueries queries)
{
    public async Task<Result<ItemDetails>> HandleAsync(GetItemDetailsQuery request, CancellationToken cancellationToken)
    {
        var item = await queries.GetByIdAsync(request.Id, cancellationToken);
        return item is null ? Result<ItemDetails>.Failure(ItemErrors.NotFound) : Result<ItemDetails>.Success(item);
    }
}
