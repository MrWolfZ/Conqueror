namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests.TopLevelProgram;

[HttpQuery]
public sealed record TopLevelTestQuery(int Payload);

public sealed record TopLevelTestQueryResponse(int Payload);

public interface ITopLevelTestQueryHandler : IQueryHandler<TopLevelTestQuery, TopLevelTestQueryResponse>
{
}

internal sealed class TopLevelTestQueryHandler : ITopLevelTestQueryHandler
{
    public async Task<TopLevelTestQueryResponse> ExecuteQuery(TopLevelTestQuery query, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return new(query.Payload + 1);
    }
}
