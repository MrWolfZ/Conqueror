using System.Threading.Tasks;

namespace Conqueror.Middleware.Polly.Messaging;

internal sealed class PollyMessageMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public required PollyMessageMiddlewareConfiguration<TMessage, TResponse> Configuration { get; init; }

    /// <inheritdoc />
    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        if (Configuration.ResiliencePipelineBuilder is null)
        {
            return await ctx.Next(ctx.Message, ctx.CancellationToken).ConfigureAwait(false);
        }

        return await Configuration.ResiliencePipelineBuilder
                                  .Build()
                                  .ExecuteAsync(async ct => await ctx.Next(ctx.Message, ct).ConfigureAwait(false), ctx.CancellationToken)
                                  .ConfigureAwait(false);
    }
}
