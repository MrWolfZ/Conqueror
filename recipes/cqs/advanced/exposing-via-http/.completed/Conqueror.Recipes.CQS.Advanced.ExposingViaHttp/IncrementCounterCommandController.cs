using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

[ApiController]
public sealed class IncrementCounterCommandController(IIncrementCounterCommandHandler handler) : ControllerBase
{
    [HttpPost("/api/custom/incrementCounter")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(IncrementCounterCommandResponse))]
    public async Task<IActionResult> ExecuteCommand(IncrementCounterCommand command, CancellationToken cancellationToken)
    {
        var response = await handler.ExecuteCommand(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
