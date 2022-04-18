using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareAttribute>
    {
        private int invocationCount;

        public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareAttribute> ctx)
            where TCommand : class
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            invocationCount += 1;
            var cmd = new TestCommand { Payload = (ctx.Command as TestCommand)!.Payload + ctx.Configuration.Increment + invocationCount + (ctx.HasUnitResponse ? 1 : 0) };
            return ctx.Next((cmd as TCommand)!, ctx.CancellationToken);
        }
    }
}
