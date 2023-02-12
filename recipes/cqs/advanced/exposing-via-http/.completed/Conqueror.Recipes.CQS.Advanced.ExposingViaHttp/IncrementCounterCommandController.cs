using Conqueror.CQS.Transport.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

[ApiController]
public sealed class IncrementCounterCommandController : ControllerBase
{
    [HttpPost("/api/custom/incrementCounter")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(IncrementCounterCommandResponse))]
    public async Task<IActionResult> ExecuteCommand(IncrementCounterCommand command, CancellationToken cancellationToken)
    {
        var response = await HttpCommandExecutor.ExecuteCommand<IncrementCounterCommand, IncrementCounterCommandResponse>(HttpContext, command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
