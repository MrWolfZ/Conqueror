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
        this ISignalPipeline<TSignal> pipeline,
        string marker)
        where TSignal : class, ISignal<TSignal>
    {
        return pipeline.Use(ctx =>
        {
            if (ctx.ConquerorContext.InProcessData.Get<bool>(
                    marker,
                    ConquerorContextDataFlowDirection.Bidirectional))
            {
                return Task.CompletedTask;
            }

            ctx.ConquerorContext.InProcessData.Set(
                marker,
                true,
                ConquerorContextDataFlowDirection.Bidirectional);

            return ctx.Next(ctx.Signal, ctx.CancellationToken);
        });
    }
}
