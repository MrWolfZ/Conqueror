using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class TestQueryHandlerWithCustomInterfaceWithExtraMethod : ITestQueryHandlerWithExtraMethod
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public void ExtraMethod()
        {
            throw new NotSupportedException();
        }
    }

    public interface ITestQueryHandlerWithExtraMethod : IQueryHandler<TestQuery, TestQueryResponse>
    {
        void ExtraMethod();
    }
}
