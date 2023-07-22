using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

internal interface IEventObserverMiddlewareInvoker
{
    Type MiddlewareType { get; }

    Task Invoke<TEvent>(TEvent evt,
                        Type observedEventType,
                        EventObserverMiddlewareNext<TEvent> next,
                        object? middlewareConfiguration,
                        IServiceProvider serviceProvider,
                        CancellationToken cancellationToken)
        where TEvent : class;
}

internal sealed class EventObserverMiddlewareInvoker<TMiddleware, TConfiguration> : IEventObserverMiddlewareInvoker
{
    public Type MiddlewareType => typeof(TMiddleware);

    public Task Invoke<TEvent>(TEvent evt,
                               Type observedEventType,
                               EventObserverMiddlewareNext<TEvent> next,
                               object? middlewareConfiguration,
                               IServiceProvider serviceProvider,
                               CancellationToken cancellationToken)
        where TEvent : class
    {
        if (typeof(TConfiguration) == typeof(NullObserverMiddlewareConfiguration))
        {
            middlewareConfiguration = new NullObserverMiddlewareConfiguration();
        }

        if (middlewareConfiguration is null)
        {
            throw new ArgumentNullException(nameof(middlewareConfiguration));
        }

        var configuration = (TConfiguration)middlewareConfiguration;

        var ctx = new DefaultEventObserverMiddlewareContext<TEvent, TConfiguration>(evt, observedEventType, next, configuration, serviceProvider, cancellationToken);

        if (typeof(TConfiguration) == typeof(NullObserverMiddlewareConfiguration))
        {
            var middleware = (IEventObserverMiddleware)serviceProvider.GetRequiredService(typeof(TMiddleware));
            return middleware.Execute(ctx);
        }

        var middlewareWithConfiguration = (IEventObserverMiddleware<TConfiguration>)serviceProvider.GetRequiredService(MiddlewareType);
        return middlewareWithConfiguration.Execute(ctx);
    }
}

internal sealed record NullObserverMiddlewareConfiguration;
