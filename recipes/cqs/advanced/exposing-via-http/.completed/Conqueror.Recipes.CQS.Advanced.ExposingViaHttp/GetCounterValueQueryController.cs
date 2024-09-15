using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

[ApiController]
public sealed class GetCounterValueQueryController(IGetCounterValueQueryHandler handler) : ControllerBase
{
    [HttpGet("/api/custom/getCounterValue")]
    public async Task<GetCounterValueQueryResponse> GetCounterValue([FromQuery] GetCounterValueQuery query, CancellationToken cancellationToken)
    {
        return await handler.Handle(query, cancellationToken);
    }
}
