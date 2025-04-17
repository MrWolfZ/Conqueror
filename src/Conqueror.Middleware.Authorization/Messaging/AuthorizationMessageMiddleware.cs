using System.Threading.Tasks;

namespace Conqueror.Middleware.Authorization.Messaging;

internal sealed class AuthorizationMessageMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public required AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse> Configuration { get; init; }

    /// <inheritdoc />
    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        var authContext = new MessageAuthorizationContext<TMessage, TResponse>(ctx.Message,
                                                                               ctx.ServiceProvider,
                                                                               ctx.ConquerorContext,
                                                                               ctx.ConquerorContext.GetCurrentPrincipal());

        foreach (var authorizationCheck in Configuration.AuthorizationChecks)
        {
            var authorizationResult = await authorizationCheck(authContext, ctx.CancellationToken).ConfigureAwait(false);

            if (authorizationResult is AuthorizationFailureResult failure)
            {
                throw new MessageAuthorizationFailedException<TMessage>(failure)
                {
                    MessagePayload = ctx.Message,
                    TransportType = ctx.TransportType,
                };
            }
        }

        return await ctx.Next(ctx.Message, ctx.CancellationToken).ConfigureAwait(false);
    }
}
