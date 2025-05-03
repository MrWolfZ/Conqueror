using System.Threading.Tasks;

namespace Conqueror.Middleware.Polly.Signalling;

internal sealed class PollySignalMiddleware<TSignal> : ISignalMiddleware<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    public required PollySignalMiddlewareConfiguration<TSignal> Configuration { get; init; }

    /// <inheritdoc />
    public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
    {
        if (Configuration.ResiliencePipelineBuilder is null)
        {
            await ctx.Next(ctx.Signal, ctx.CancellationToken).ConfigureAwait(false);

            return;
        }

        await Configuration.ResiliencePipelineBuilder
                           .Build()
                           .ExecuteAsync(async ct => await ctx.Next(ctx.Signal, ct).ConfigureAwait(false), ctx.CancellationToken)
                           .ConfigureAwait(false);
    }
}
