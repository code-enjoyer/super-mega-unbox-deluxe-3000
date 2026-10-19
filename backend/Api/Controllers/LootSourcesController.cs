using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SuperMegaUnboxDeluxe.Api.Contracts;
using SuperMegaUnboxDeluxe.Application.Items.GetLootSources;

namespace SuperMegaUnboxDeluxe.Api.Controllers;

[ApiController]
[Route("loot-sources")]
public sealed class LootSourcesController(GetLootSourcesHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LootSource[]>> GetAsync(CancellationToken cancellationToken)
    {
        var sources = await handler.HandleAsync(new GetLootSourcesQuery(), cancellationToken);
        return Ok(sources.Select(source => new LootSource(source.Id, source.Name, source.Description)).ToArray());
    }
}
