using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    [HttpQuery]
    public sealed record TestQueryWithoutPayload;

    public sealed class TestQueryWithoutPayloadHandler : ITestQueryWithoutPayloadHandler
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public interface ITestQueryWithoutPayloadHandler : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
    {
    }
}
