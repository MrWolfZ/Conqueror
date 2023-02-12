using Conqueror.CQS.Transport.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

[ApiController]
public sealed class IncrementCounterByCommandController : ControllerBase
{
    [HttpPost("/api/custom/incrementCounterBy")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(IncrementCounterByCommandResponse))]
    public async Task<IActionResult> ExecuteCommand(IncrementCounterByCommand command, CancellationToken cancellationToken)
    {
        var response = await HttpCommandExecutor.ExecuteCommand<IncrementCounterByCommand, IncrementCounterByCommandResponse>(HttpContext, command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
