namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests.TopLevelProgram;

[HttpQuery]
public sealed record TopLevelTestQuery(int Payload);

public sealed record TopLevelTestQueryResponse(int Payload);

public interface ITopLevelTestQueryHandler : IQueryHandler<TopLevelTestQuery, TopLevelTestQueryResponse>;

internal sealed class TopLevelTestQueryHandler : ITopLevelTestQueryHandler
{
    public async Task<TopLevelTestQueryResponse> ExecuteQuery(TopLevelTestQuery query, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new(query.Payload + 1);
    }
}
