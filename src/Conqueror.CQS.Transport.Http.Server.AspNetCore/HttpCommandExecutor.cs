using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

internal static class HttpCommandExecutor
{
    public static Task<TResponse> Execute<TCommand, TResponse>(HttpContext httpContext, TCommand command, CancellationToken cancellationToken)
        where TCommand : class
    {
        var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return commandHandler.Handle(command, cancellationToken);
    }

    public static Task Execute<TCommand>(HttpContext httpContext, TCommand command, CancellationToken cancellationToken)
        where TCommand : class
    {
        var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
        return commandHandler.Handle(command, cancellationToken);
    }

    public static Task<TResponse> Execute<TCommand, TResponse>(HttpContext httpContext, CancellationToken cancellationToken)
        where TCommand : class, new()
    {
        var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return commandHandler.Handle(new(), cancellationToken);
    }

    public static Task Execute<TCommand>(HttpContext httpContext, CancellationToken cancellationToken)
        where TCommand : class, new()
    {
        var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
        return commandHandler.Handle(new(), cancellationToken);
    }
}
