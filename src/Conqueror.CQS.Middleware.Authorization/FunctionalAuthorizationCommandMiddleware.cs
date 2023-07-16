using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     A command middleware which adds functional authorization functionality to a command pipeline.
/// </summary>
public sealed class FunctionalAuthorizationCommandMiddleware : ICommandMiddleware<FunctionalAuthorizationCommandMiddlewareConfiguration>
{
    private readonly IConquerorAuthenticationContext authenticationContext;

    public FunctionalAuthorizationCommandMiddleware(IConquerorAuthenticationContext authenticationContext)
    {
        this.authenticationContext = authenticationContext;
    }

    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, FunctionalAuthorizationCommandMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        if (authenticationContext.CurrentPrincipal is { Identity: { IsAuthenticated: true } identity } principal)
        {
            var result = await ctx.Configuration.AuthorizationCheck(principal, typeof(TCommand)).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                throw new ConquerorFunctionalAuthorizationFailedException($"principal '{identity.Name}' is not authorized to execute command '{typeof(TCommand).Name}'", result);
            }
        }

        return await ctx.Next(ctx.Command, ctx.CancellationToken).ConfigureAwait(false);
    }
}
