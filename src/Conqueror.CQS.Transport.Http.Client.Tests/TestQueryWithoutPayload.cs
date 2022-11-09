namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [HttpQuery]
    public sealed record TestQueryWithoutPayload;

    public sealed class TestQueryWithoutPayloadHandler : ITestQueryWithoutPayloadHandler
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public interface ITestQueryWithoutPayloadHandler : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
    {
    }
}
