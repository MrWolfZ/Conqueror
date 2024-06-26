using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandMiddlewareInvoker<TMiddleware, TConfiguration> : ICommandMiddlewareInvoker
{
    public Type MiddlewareType => typeof(TMiddleware);

    public Task<TResponse> Invoke<TCommand, TResponse>(TCommand command,
                                                       CommandMiddlewareNext<TCommand, TResponse> next,
                                                       object? middlewareConfiguration,
                                                       IServiceProvider serviceProvider,
                                                       IConquerorContext conquerorContext,
                                                       CancellationToken cancellationToken)
        where TCommand : class
    {
        if (typeof(TConfiguration) == typeof(NullMiddlewareConfiguration))
        {
            middlewareConfiguration = new NullMiddlewareConfiguration();
        }

        if (middlewareConfiguration is null)
        {
            throw new ArgumentNullException(nameof(middlewareConfiguration));
        }

        var configuration = (TConfiguration)middlewareConfiguration;

        var ctx = new DefaultCommandMiddlewareContext<TCommand, TResponse, TConfiguration>(command, next, configuration, serviceProvider, conquerorContext, cancellationToken);

        if (typeof(TConfiguration) == typeof(NullMiddlewareConfiguration))
        {
            var middleware = (ICommandMiddleware)serviceProvider.GetRequiredService(typeof(TMiddleware));
            return middleware.Execute(ctx);
        }

        var middlewareWithConfiguration = (ICommandMiddleware<TConfiguration>)serviceProvider.GetRequiredService(typeof(TMiddleware));
        return middlewareWithConfiguration.Execute(ctx);
    }
}

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "It is fine for a null-object to be empty.")]
internal sealed record NullMiddlewareConfiguration;
