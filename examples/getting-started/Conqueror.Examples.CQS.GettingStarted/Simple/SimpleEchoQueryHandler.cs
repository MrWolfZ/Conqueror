namespace Conqueror.Examples.CQS.GettingStarted.Simple;

public sealed record SimpleEchoQuery(int Parameter);

public sealed record SimpleEchoQueryResponse(int Value);

public sealed class SimpleEchoQueryHandler : IQueryHandler<SimpleEchoQuery, SimpleEchoQueryResponse>
{
    public async Task<SimpleEchoQueryResponse> ExecuteQuery(SimpleEchoQuery query, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new(query.Parameter);
    }
}
