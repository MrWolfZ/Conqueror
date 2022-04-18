using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareAttribute>
    {
        private int invocationCount;

        public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareAttribute> ctx)
            where TQuery : class
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            invocationCount += 1;
            var cmd = new TestQuery { Payload = (ctx.Query as TestQuery)!.Payload + ctx.Configuration.Increment + invocationCount };
            return ctx.Next((cmd as TQuery)!, ctx.CancellationToken);
        }
    }
}
