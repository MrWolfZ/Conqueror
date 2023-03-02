using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

public static class HttpCommandExecutor
{
    public static Task<TResponse> ExecuteCommand<TCommand, TResponse>(HttpContext httpContext, TCommand command, CancellationToken cancellationToken)
        where TCommand : class
    {
        return HttpRequestExecutor.ExecuteWithContext(httpContext, async () =>
        {
            var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
            return await commandHandler.ExecuteCommand(command, cancellationToken).ConfigureAwait(false);
        });
    }

    public static Task ExecuteCommand<TCommand>(HttpContext httpContext, TCommand command, CancellationToken cancellationToken)
        where TCommand : class
    {
        return HttpRequestExecutor.ExecuteWithContext(httpContext, async () =>
        {
            var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
            await commandHandler.ExecuteCommand(command, cancellationToken).ConfigureAwait(false);
            return UnitCommandResponse.Instance;
        });
    }

    public static Task<TResponse> ExecuteCommand<TCommand, TResponse>(HttpContext httpContext, CancellationToken cancellationToken)
        where TCommand : class, new()
    {
        return HttpRequestExecutor.ExecuteWithContext(httpContext, async () =>
        {
            var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
            return await commandHandler.ExecuteCommand(new(), cancellationToken).ConfigureAwait(false);
        });
    }

    public static Task ExecuteCommand<TCommand>(HttpContext httpContext, CancellationToken cancellationToken)
        where TCommand : class, new()
    {
        return HttpRequestExecutor.ExecuteWithContext(httpContext, async () =>
        {
            var commandHandler = httpContext.RequestServices.GetRequiredService<ICommandHandler<TCommand>>();
            await commandHandler.ExecuteCommand(new(), cancellationToken).ConfigureAwait(false);
            return UnitCommandResponse.Instance;
        });
    }
}
