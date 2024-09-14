using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     A command middleware which adds operation type authorization functionality to a command pipeline.
/// </summary>
public sealed class OperationTypeAuthorizationCommandMiddleware : ICommandMiddleware
{
    public required OperationTypeAuthorizationCommandMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        var authenticationContext = new ConquerorAuthenticationContext();
        if (authenticationContext.CurrentPrincipal is { Identity: { IsAuthenticated: true } identity } principal)
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
