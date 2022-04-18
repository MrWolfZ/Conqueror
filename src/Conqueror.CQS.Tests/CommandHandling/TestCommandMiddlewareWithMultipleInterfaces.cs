using System;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandMiddlewareWithMultipleInterfaces : ICommandMiddleware<TestCommandMiddlewareAttribute>, ICommandMiddleware<TestCommandMiddlewareThatShouldNeverBeCalledAttribute>
    {
        public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareAttribute> ctx)
            where TCommand : class
        {
            throw new InvalidOperationException("this middleware should never be called");
        }

        public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareThatShouldNeverBeCalledAttribute> ctx)
            where TCommand : class
        {
            throw new InvalidOperationException("this middleware should never be called");
        }
    }
}
