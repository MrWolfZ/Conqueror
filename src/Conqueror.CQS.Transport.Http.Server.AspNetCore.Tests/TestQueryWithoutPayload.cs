namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [HttpQuery]
    public sealed record TestQueryWithoutPayload;

    public sealed class TestQueryHandlerWithoutPayload : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }
}
