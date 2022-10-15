namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
{
    [HttpQuery(UsePost = true)]
    public sealed record TestPostQuery
    {
        public int Payload { get; init; }
    }

    public sealed class TestPostQueryHandler : ITestPostQueryHandler
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public interface ITestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>
    {
    }
}
