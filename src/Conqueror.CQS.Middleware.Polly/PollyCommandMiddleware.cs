using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Polly;

/// <summary>
///     A command middleware which adds data annotation validation functionality to a command pipeline.
/// </summary>
public sealed class PollyCommandMiddleware : ICommandMiddleware<PollyCommandMiddlewareConfiguration>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, PollyCommandMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        TResponse response = default!;

        await ctx.Configuration.Policy.ExecuteAsync(async () =>
        {
            // we cannot get the caller to give us a AsyncPolicy<TResponse>, so we need to capture
            // the response ourselves
            response = await ctx.Next(ctx.Command, ctx.CancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        return response;
    }
}
