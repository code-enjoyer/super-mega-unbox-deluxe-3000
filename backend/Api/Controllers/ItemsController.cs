using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SuperMegaUnboxDeluxe.Api.Contracts;
using SuperMegaUnboxDeluxe.Api.ErrorHandling;
using SuperMegaUnboxDeluxe.Application.Items.GenerateItem;
using SuperMegaUnboxDeluxe.Application.Items.GetItemDetails;

namespace SuperMegaUnboxDeluxe.Api.Controllers;

[ApiController]
[Route("items")]
public sealed class ItemsController(GetItemDetailsHandler details, GenerateItemHandler generate) : ControllerBase
{
    [HttpGet("{id:guid}", Name = Constants.Actions.GetItemDetails)]
    public async Task<ActionResult<Item>> GetItemDetailsAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await details.HandleAsync(new GetItemDetailsQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value.ToContract()) : this.ToProblem(result.Error!);
    }

    [HttpPost(Name = Constants.Actions.GenerateItem)]
    public async Task<ActionResult<Item>> GenerateItemAsync([FromBody] GenerateItemRequest? request, CancellationToken cancellationToken)
    {
        var result = await generate.HandleAsync(new GenerateItemCommand(request?.LootSourceId), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(Constants.Actions.GetItemDetails, new { id = result.Value.Id }, result.Value.ToContract())
            : this.ToProblem(result.Error!);
    }
}
