using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class TestQueryHandlerWithMultipleInterfaces : IQueryHandler<TestQuery, TestQueryResponse>, IQueryHandler<object, TestQueryResponse>
    {
        public Task<TestQueryResponse> ExecuteQuery(object query, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
