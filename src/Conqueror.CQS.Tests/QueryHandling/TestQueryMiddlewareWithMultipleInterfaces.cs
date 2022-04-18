using System;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class TestQueryMiddlewareWithMultipleInterfaces : IQueryMiddleware<TestQueryMiddlewareAttribute>, IQueryMiddleware<TestQueryMiddlewareThatShouldNeverBeCalledAttribute>
    {
        public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareAttribute> ctx)
            where TQuery : class
        {
            throw new InvalidOperationException("this middleware should never be called");
        }

        public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareThatShouldNeverBeCalledAttribute> ctx)
            where TQuery : class
        {
            throw new InvalidOperationException("this middleware should never be called");
        }
    }
}
