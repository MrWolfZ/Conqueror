namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [HttpQuery(UsePost = true)]
    public sealed record TestPostQueryWithoutPayload;

    public sealed class TestPostQueryHandlerWithoutPayload : IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithoutPayload query, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }
}
