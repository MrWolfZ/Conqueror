using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Having the interface for the type at the top makes sense, but we want to keep the file named after the proper type, not the interface.")]
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

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "It is fine for a null-object to be empty.")]
internal sealed record NullObserverMiddlewareConfiguration;
