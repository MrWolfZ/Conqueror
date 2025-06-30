using System;
using System.Collections;
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

    ISignalPipeline<TSignal> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : ISignalMiddleware<TSignal>;

    ISignalPipeline<TSignal> Use(SignalMiddlewareFn<TSignal> middlewareFn);

    ISignalPipeline<TSignal> Without<TMiddleware>()
        where TMiddleware : ISignalMiddleware<TSignal>;

    ISignalPipeline<TSignal> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : ISignalMiddleware<TSignal>;
}

public static class SignalPipelineConditionalExtensions
{
    public static ISignalPipeline<TSignal> UseWhen<TSignal>(
        this ISignalPipeline<TSignal> pipeline,
        Predicate<SignalMiddlewareContext<TSignal>> predicate)
        where TSignal : class, ISignal<TSignal>
    {
        return new ConditionalPipeline<TSignal>(predicate, pipeline);
    }

    internal sealed class ConditionalSignalMiddleware<TSignal, TMiddleware>(
        Predicate<SignalMiddlewareContext<TSignal>> predicate,
        TMiddleware middleware)
        : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
        where TMiddleware : ISignalMiddleware<TSignal>
    {
        public TMiddleware Middleware => middleware;

        public Task Execute(SignalMiddlewareContext<TSignal> ctx)
            => predicate(ctx) ? middleware.Execute(ctx) : ctx.Next(ctx.Signal, ctx.CancellationToken);
    }

    private sealed class ConditionalDelegateSignalMiddleware<TSignal>(
        Predicate<SignalMiddlewareContext<TSignal>> predicate,
        SignalMiddlewareFn<TSignal> middlewareFn)
        : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public Task Execute(SignalMiddlewareContext<TSignal> ctx)
            => predicate(ctx) ? middlewareFn(ctx) : ctx.Next(ctx.Signal, ctx.CancellationToken);
    }

    private sealed class ConditionalPipeline<TSignal>(
        Predicate<SignalMiddlewareContext<TSignal>> predicate,
        ISignalPipeline<TSignal> pipeline)
        : ISignalPipeline<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public Type? HandlerType => pipeline.HandlerType;

        public IServiceProvider ServiceProvider => pipeline.ServiceProvider;

        public int Count => pipeline.Count;

        public ISignalPipeline<TSignal> Use<TMiddleware>(TMiddleware middleware)
            where TMiddleware : ISignalMiddleware<TSignal>
        {
            _ = pipeline.Use(new ConditionalSignalMiddleware<TSignal, TMiddleware>(predicate, middleware));

            return this;
        }

        public ISignalPipeline<TSignal> Use(SignalMiddlewareFn<TSignal> middlewareFn)
        {
            _ = pipeline.Use(new ConditionalDelegateSignalMiddleware<TSignal>(predicate, middlewareFn));

            return this;
        }

        ISignalPipeline<TSignal> ISignalPipeline<TSignal>.Without<TMiddleware>()
            => pipeline.Without<TMiddleware>();

        ISignalPipeline<TSignal> ISignalPipeline<TSignal>.Configure<TMiddleware>(Action<TMiddleware> configure)
            => pipeline.Configure(configure);

        IEnumerator<ISignalMiddleware<TSignal>> IEnumerable<ISignalMiddleware<TSignal>>.GetEnumerator()
            => pipeline.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)pipeline).GetEnumerator();
    }
}
