using System.Linq;
using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     A command middleware which adds payload authorization functionality to a command pipeline.
/// </summary>
public sealed class PayloadAuthorizationCommandMiddleware : ICommandMiddleware
{
    public PayloadAuthorizationCommandMiddlewareConfiguration Configuration { get; } = new();

    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        var authenticationContext = new ConquerorAuthenticationContext();
        if (authenticationContext.CurrentPrincipal is { Identity: { IsAuthenticated: true } identity } principal)
        {
            var results = await Task.WhenAll(Configuration.AuthorizationChecks.Select(c => c(principal, ctx.Command))).ConfigureAwait(false);
            var failures = results.Where(r => !r.IsSuccess).ToList();

            if (failures.Any())
            {
                var aggregatedFailure = failures.Count == 1 ? failures[0] : ConquerorAuthorizationResult.Failure(failures.SelectMany(f => f.FailureReasons).ToList());
                throw new ConquerorOperationPayloadAuthorizationFailedException($"principal '{identity.Name}' is not authorized to execute command of type '{typeof(TCommand).Name}'", aggregatedFailure);
            }
        }

        return await ctx.Next(ctx.Command, ctx.CancellationToken).ConfigureAwait(false);
    }
}
