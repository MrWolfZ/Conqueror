namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [HttpQuery]
    public sealed record TestQueryWithCollectionPayload
    {
        public List<int> Payload { get; init; } = new();
    }

    public sealed record TestQueryWithCollectionPayloadResponse
    {
        public List<int> Payload { get; init; } = new();
    }

    public sealed class TestQueryWithCollectionPayloadHandler : ITestQueryWithCollectionPayloadHandler
    {
        public async Task<TestQueryWithCollectionPayloadResponse> ExecuteQuery(TestQueryWithCollectionPayload query, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = new(query.Payload) { 1 } };
        }
    }

    public interface ITestQueryWithCollectionPayloadHandler : IQueryHandler<TestQueryWithCollectionPayload, TestQueryWithCollectionPayloadResponse>
    {
    }
}
