using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class TestQueryHandlerWithMiddleware : IQueryHandler<TestQuery, TestQueryResponse>
    {
        [TestQueryMiddleware(Increment = 2)]
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new() { Payload = query.Payload + 1 };
        }
    }
}
