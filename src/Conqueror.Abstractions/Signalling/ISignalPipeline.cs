using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CA1034

// ReSharper disable once CheckNamespace
namespace Conqueror;

public delegate Task SignalMiddlewareFn<TSignal>(SignalMiddlewareContext<TSignal> context)
    where TSignal : class, ISignal<TSignal>;

public interface ISignalPipeline<TSignal> : IReadOnlyCollection<ISignalMiddleware<TSignal>>
    where TSignal : class, ISignal<TSignal>
{
    /// <summary>
    ///     The type of the handler this pipeline is being built for. Is <c>null</c> for
    ///     delegate handlers or when the pipeline is being built for a publisher.
    /// </summary>
    Type? HandlerType { get; }

    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }

    /// <summary>
    ///     The transport type this pipeline is being built for. This property can be useful
    ///     to build pipeline extension methods that should include certain middlewares only
    ///     for specific transports (e.g. including a logging middleware only if the transport
    ///     is not the default in-process transport to prevent duplicate log entries).
    /// </summary>
    SignalTransportType TransportType { get; }

    ISignalPipeline<TSignal> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : ISignalMiddleware<TSignal>;

    ISignalPipeline<TSignal> Use(SignalMiddlewareFn<TSignal> middlewareFn);

    ISignalPipeline<TSignal> Without<TMiddleware>()
        where TMiddleware : ISignalMiddleware<TSignal>;

    ISignalPipeline<TSignal> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : ISignalMiddleware<TSignal>;
}
