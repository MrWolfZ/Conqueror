using System.Threading.Tasks;

namespace Conqueror;

public interface ICommandMiddleware<TCommand, TResponse>
    where TCommand : class
{
    Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx);
}
