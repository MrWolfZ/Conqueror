namespace Conqueror.Examples.CQS.GettingStarted.HttpExample;

[HttpQuery]
public sealed record HttpExampleQuery(int Parameter);

public sealed record HttpExampleQueryResponse(int Value);

public interface IHttpExampleQueryHandler : IQueryHandler<HttpExampleQuery, HttpExampleQueryResponse>
{
}

public sealed class HttpExampleQueryHandler : IHttpExampleQueryHandler
{
    public async Task<HttpExampleQueryResponse> ExecuteQuery(HttpExampleQuery command, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new(command.Parameter);
    }
}