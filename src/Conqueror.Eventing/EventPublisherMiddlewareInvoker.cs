using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Having the interface for the type at the top makes sense, but we want to keep the file named after the proper type, not the interface.")]
internal interface IEventPublisherMiddlewareInvoker
{
    Type MiddlewareType { get; }

    Task Invoke<TEvent>(TEvent evt,
                        EventPublisherMiddlewareNext<TEvent> next,
                        object? middlewareConfiguration,
                        IServiceProvider serviceProvider,
                        CancellationToken cancellationToken)
        where TEvent : class;
}

internal sealed class EventPublisherMiddlewareInvoker<TMiddleware, TConfiguration> : IEventPublisherMiddlewareInvoker
{
    public Type MiddlewareType => typeof(TMiddleware);

    public Task Invoke<TEvent>(TEvent evt,
                               EventPublisherMiddlewareNext<TEvent> next,
                               object? middlewareConfiguration,
                               IServiceProvider serviceProvider,
                               CancellationToken cancellationToken)
        where TEvent : class
    {
        if (typeof(TConfiguration) == typeof(NullPublisherMiddlewareConfiguration))
        {
            middlewareConfiguration = new NullPublisherMiddlewareConfiguration();
        }

        if (middlewareConfiguration is null)
        {
            throw new ArgumentNullException(nameof(middlewareConfiguration));
        }

        var configuration = (TConfiguration)middlewareConfiguration;

        var ctx = new DefaultEventPublisherMiddlewareContext<TEvent, TConfiguration>(evt, next, configuration, serviceProvider, cancellationToken);

        if (typeof(TConfiguration) == typeof(NullPublisherMiddlewareConfiguration))
        {
            var middleware = (IEventPublisherMiddleware)serviceProvider.GetRequiredService(typeof(TMiddleware));
            return middleware.Execute(ctx);
        }

        var middlewareWithConfiguration = (IEventPublisherMiddleware<TConfiguration>)serviceProvider.GetRequiredService(MiddlewareType);
        return middlewareWithConfiguration.Execute(ctx);
    }
}

internal sealed record NullPublisherMiddlewareConfiguration;
