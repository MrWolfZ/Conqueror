using System.Threading.Tasks;

namespace Conqueror;

public interface ICommandMiddleware
{
    Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class;
}
