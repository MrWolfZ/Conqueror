namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    public sealed record NonHttpTestQuery
    {
        public int Payload { get; init; }
    }

    public sealed class NonHttpTestQueryHandler : INonHttpTestQueryHandler
    {
        public Task<TestQueryResponse> ExecuteQuery(NonHttpTestQuery query, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    public interface INonHttpTestQueryHandler : IQueryHandler<NonHttpTestQuery, TestQueryResponse>
    {
    }
}
