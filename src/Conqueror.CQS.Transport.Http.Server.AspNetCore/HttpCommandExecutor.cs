using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

internal static class HttpCommandExecutor
{
    public static Task<TResponse> ExecuteCommand<TCommand, TResponse>(HttpContext httpContext, TCommand command, CancellationToken cancellationToken)
        where TCommand : class
    {
        var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return commandHandler.ExecuteCommand(command, cancellationToken);
    }

    public static Task ExecuteCommand<TCommand>(HttpContext httpContext, TCommand command, CancellationToken cancellationToken)
        where TCommand : class
    {
        var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
        return commandHandler.ExecuteCommand(command, cancellationToken);
    }

    public static Task<TResponse> ExecuteCommand<TCommand, TResponse>(HttpContext httpContext, CancellationToken cancellationToken)
        where TCommand : class, new()
    {
        var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return commandHandler.ExecuteCommand(new(), cancellationToken);
    }

    public static Task ExecuteCommand<TCommand>(HttpContext httpContext, CancellationToken cancellationToken)
        where TCommand : class, new()
    {
        var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
        return commandHandler.ExecuteCommand(new(), cancellationToken);
    }
}
