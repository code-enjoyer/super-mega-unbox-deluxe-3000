using Microsoft.AspNetCore.Mvc;
using SuperMegaUnboxDeluxe.Api.Contracts;

namespace SuperMegaUnboxDeluxe.Api.Controllers;

[ApiController]
[Route("api/status")]
public sealed class StatusController : ControllerBase
{
    [HttpGet(Name = Constants.Actions.GetApiStatus)]
    public ActionResult<ApiStatusDetails> GetApiStatus()
    {
        const string okStatus = "ok";

        return Ok(new ApiStatusDetails { Status = okStatus });
    }
}
