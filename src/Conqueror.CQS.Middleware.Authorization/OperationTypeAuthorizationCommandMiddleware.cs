using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     A command middleware which adds operation type authorization functionality to a command pipeline.
/// </summary>
public sealed class OperationTypeAuthorizationCommandMiddleware : ICommandMiddleware<OperationTypeAuthorizationCommandMiddlewareConfiguration>
{
    private readonly IConquerorAuthenticationContext authenticationContext;

    public OperationTypeAuthorizationCommandMiddleware(IConquerorAuthenticationContext authenticationContext)
    {
        this.authenticationContext = authenticationContext;
    }

    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, OperationTypeAuthorizationCommandMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        if (authenticationContext.CurrentPrincipal is { Identity: { IsAuthenticated: true } identity } principal)
        {
            var result = await ctx.Configuration.AuthorizationCheck(principal, typeof(TCommand)).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                throw new ConquerorOperationTypeAuthorizationFailedException($"principal '{identity.Name}' is not authorized to execute command type '{typeof(TCommand).Name}'", result);
            }
        }

        return await ctx.Next(ctx.Command, ctx.CancellationToken).ConfigureAwait(false);
    }
}
