using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Polly;

/// <summary>
///     A command middleware which adds data annotation validation functionality to a command pipeline.
/// </summary>
public sealed class PollyCommandMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
{
    public required PollyCommandMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        TResponse response = default!;

        await Configuration.Policy.ExecuteAsync(async () =>
        {
            // we cannot get the caller to give us a AsyncPolicy<TResponse>, so we need to capture
            // the response ourselves
            response = await ctx.Next(ctx.Command, ctx.CancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        return response;
    }
}
