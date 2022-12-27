namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [HttpQuery]
    public sealed record TestQuery2;

    public sealed record TestQueryResponse2;

    public sealed class TestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>
    {
        public Task<TestQueryResponse2> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
