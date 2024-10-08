using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     A command middleware which adds operation type authorization functionality to a command pipeline.
/// </summary>
public sealed class OperationTypeAuthorizationCommandMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
{
    public required OperationTypeAuthorizationCommandMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        if (ctx.ConquerorContext.GetCurrentPrincipal() is { Identity: { IsAuthenticated: true } identity } principal)
        {
            var result = await Configuration.AuthorizationCheck(principal, typeof(TCommand)).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                throw new ConquerorOperationTypeAuthorizationFailedException($"principal '{identity.Name}' is not authorized to execute command type '{typeof(TCommand).Name}'", result);
            }
        }

        return await ctx.Next(ctx.Command, ctx.CancellationToken).ConfigureAwait(false);
    }
}
