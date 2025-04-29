using Conqueror;

namespace Quickstart.Enhanced;

public static class SignalPipelineExtensions
{
    public static ISignalPipeline<TSignal> SkipSignalMatching<TSignal, TSignalMatch>(
        this ISignalPipeline<TSignal> pipeline,
        Predicate<TSignalMatch> predicate)
        where TSignal : class, ISignal<TSignal>
        where TSignalMatch : class, ISignal<TSignalMatch>
    {
        return pipeline.Use(ctx =>
        {
            if (ctx.Signal is TSignalMatch signalMatch && predicate(signalMatch))
            {
                return Task.CompletedTask;
            }

            return ctx.Next(ctx.Signal, ctx.CancellationToken);
        });
    }

    public static ISignalPipeline<TSignal> EnsureSingleExecutionPerOperation<TSignal>(
        this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        return pipeline.Use(ctx =>
        {
            if (ctx.ConquerorContext.ContextData.Get<bool>("executed"))
            {
                return Task.CompletedTask;
            }

            ctx.ConquerorContext.ContextData.Set("executed", true);

            return ctx.Next(ctx.Signal, ctx.CancellationToken);
        });
    }
}
