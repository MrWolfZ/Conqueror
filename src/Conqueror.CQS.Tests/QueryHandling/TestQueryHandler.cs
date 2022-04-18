using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class TestQueryHandler : ITestQueryHandler
    {
        private int invocationCount;

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
        {
            invocationCount += 1;
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + invocationCount };
        }
    }

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
    }
}
