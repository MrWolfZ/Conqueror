using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class TestQueryHandlerWithMultipleCustomInterfaces : ITestQueryHandler, ITestQueryHandler2
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

    public interface ITestQueryHandler2 : IQueryHandler<object, TestQueryResponse>
    {
    }
}
