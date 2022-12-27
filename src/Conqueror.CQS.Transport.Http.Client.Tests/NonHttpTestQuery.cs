﻿namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    public sealed record NonHttpTestQuery
    {
        public int Payload { get; init; }
    }

    public sealed class NonHttpTestQueryHandler : INonHttpTestQueryHandler
    {
        public Task<TestQueryResponse> ExecuteQuery(NonHttpTestQuery query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public interface INonHttpTestQueryHandler : IQueryHandler<NonHttpTestQuery, TestQueryResponse>
    {
    }
}
