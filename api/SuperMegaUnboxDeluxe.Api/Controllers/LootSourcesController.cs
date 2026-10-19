using Microsoft.AspNetCore.Mvc;
using SuperMegaUnboxDeluxe.Api.Contracts;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using System.Linq;

namespace SuperMegaUnboxDeluxe.Api.Controllers;

[ApiController]
[Route("loot-sources")]
public sealed class LootSourcesController : ControllerBase
{
    private readonly IItemCatalog _catalog;

    public LootSourcesController(IItemCatalog catalog) => _catalog = catalog;

    [HttpGet]
    public ActionResult<LootSource[]> Get() => Ok(_catalog.Sources
        .Select(x => new LootSource(x.Id, x.Name, x.Description)).ToArray());
}
