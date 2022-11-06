namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [HttpQuery(UsePost = true)]
    public sealed record TestPostQueryWithoutPayload;

    public sealed class TestPostQueryWithoutPayloadHandler : ITestPostQueryWithoutPayloadHandler
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithoutPayload query, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public interface ITestPostQueryWithoutPayloadHandler : IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse>
    {
    }
}
