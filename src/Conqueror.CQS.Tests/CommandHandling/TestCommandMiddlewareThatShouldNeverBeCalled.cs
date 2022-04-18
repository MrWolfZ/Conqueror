using System;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandMiddlewareThatShouldNeverBeCalled : ICommandMiddleware<TestCommandMiddlewareThatShouldNeverBeCalledAttribute>
    {
        public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareThatShouldNeverBeCalledAttribute> ctx)
            where TCommand : class
        {
            throw new InvalidOperationException("this middleware should never be called");
        }
    }
}
