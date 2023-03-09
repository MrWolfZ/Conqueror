using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

[ApiController]
public sealed class GetCounterValueQueryController : ControllerBase
{
    private readonly IGetCounterValueQueryHandler handler;

    public GetCounterValueQueryController(IGetCounterValueQueryHandler handler)
    {
        this.handler = handler;
    }

    [HttpGet("/api/custom/getCounterValue")]
    public async Task<GetCounterValueQueryResponse> ExecuteQuery([FromQuery] GetCounterValueQuery query, CancellationToken cancellationToken)
    {
        return await handler.ExecuteQuery(query, cancellationToken);
    }
}
