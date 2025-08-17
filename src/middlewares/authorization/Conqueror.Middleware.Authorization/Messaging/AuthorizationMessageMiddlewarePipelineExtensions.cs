#pragma warning disable IDE0130 // Namespaces don't match folder structure - we want these extensions to be accessible from client registration code without an extra import

namespace Conqueror;

using Middleware.Authorization.Messaging;

/// <summary>
///     Extension methods for <see cref="IMessagePipeline{TMessage,TResponse}" /> to add, configure, or remove
///     authorization functionality.
/// </summary>
public static class AuthorizationMessageMiddlewarePipelineExtensions
{
    /// <summary>
    ///     Add authorization functionality to a message pipeline.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="pipeline">The message pipeline to add authorization to</param>
    /// <param name="configure">
    ///     An optional delegate to configure the authorization functionality (see
    ///     <see cref="AuthorizationMessageMiddlewareConfiguration{TMessage,TResponse}" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The message pipeline</returns>
    public static IMessagePipeline<TMessage, TResponse> UseAuthorization<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        Action<AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse>>? configure = null
    )
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var configuration = new AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse>();
        configure?.Invoke(configuration);

        return pipeline.Use(new AuthorizationMessageMiddleware<TMessage, TResponse> { Configuration = configuration });
    }

    /// <summary>
    ///     Configure the authorization middleware added to a message pipeline.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="pipeline">The message pipeline with the authorization middleware to configure</param>
    /// <param name="configure">
    ///     The delegate for configuring the authorization functionality (see
    ///     <see cref="AuthorizationMessageMiddlewareConfiguration{TMessage,TResponse}" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The message pipeline</returns>
    public static IMessagePipeline<TMessage, TResponse> ConfigureAuthorization<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        Action<AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse>> configure
    )
        where TMessage : class, IMessage<TMessage, TResponse> =>
        pipeline.Configure<AuthorizationMessageMiddleware<TMessage, TResponse>>(m => configure(m.Configuration));

    /// <summary>
    ///     Remove the authorization middleware from a message pipeline.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="pipeline">The message pipeline with the authorization middleware to remove</param>
    /// <returns>The message pipeline</returns>
    public static IMessagePipeline<TMessage, TResponse> WithoutAuthorization<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline
    )
        where TMessage : class, IMessage<TMessage, TResponse> =>
        pipeline.Without<AuthorizationMessageMiddleware<TMessage, TResponse>>();
}
