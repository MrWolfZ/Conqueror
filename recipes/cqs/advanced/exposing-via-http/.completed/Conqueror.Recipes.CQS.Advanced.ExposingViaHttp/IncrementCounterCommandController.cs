using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

[ApiController]
public sealed class IncrementCounterCommandController(IIncrementCounterCommandHandler handler) : ControllerBase
{
    [HttpPost("/api/custom/incrementCounter")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(IncrementCounterCommandResponse))]
    public async Task<IActionResult> IncrementCounter(IncrementCounterCommand command, CancellationToken cancellationToken)
    {
        var response = await handler.Handle(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
