using System.Threading.Tasks;

// empty interface used as marker interface for other operations
#pragma warning disable CA1040

namespace Conqueror
{
    public interface ICommandMiddleware
    {
        Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class;
    }

    public interface ICommandMiddleware<TConfiguration>
    {
        Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TConfiguration> ctx)
            where TCommand : class;
    }
}
