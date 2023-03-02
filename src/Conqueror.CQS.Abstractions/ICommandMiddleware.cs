using System.Threading.Tasks;

namespace Conqueror;

public interface ICommandMiddleware : ICommandMiddlewareMarker
{
    Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class;
}

public interface ICommandMiddleware<TConfiguration> : ICommandMiddlewareMarker
{
    Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TConfiguration> ctx)
        where TCommand : class;
}

public interface ICommandMiddlewareMarker
{
}
