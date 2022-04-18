using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
{
    [HttpQuery]
    public sealed record TestQueryWithoutPayload;

    public sealed class TestQueryHandlerWithoutPayload : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }
}
