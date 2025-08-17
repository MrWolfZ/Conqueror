namespace Conqueror.Streaming;

internal sealed class StreamProducerMiddlewareInvoker<TMiddleware, TConfiguration> : IStreamProducerMiddlewareInvoker
{
    public Type MiddlewareType => typeof(TMiddleware);

    [SuppressMessage(
        "Minor Bug",
        "S1226:Method parameters, caught exceptions and foreach variables\' initial values should not be ignored",
        Justification = "we are not really ignoring the value, we are just setting it in a special situation"
    )]
    [SuppressMessage(
        "Roslynator",
        "RCS1256:Invalid argument null check",
        Justification = "the parameter is only considered nullable for a specific situation, so we still need to check it"
    )]
    public IAsyncEnumerable<TItem> Invoke<TRequest, TItem>(
        TRequest request,
        StreamProducerMiddlewareNext<TRequest, TItem> next,
        object? middlewareConfiguration,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    )
        where TRequest : class
    {
        if (typeof(TConfiguration) == typeof(NullStreamProducerMiddlewareConfiguration))
        {
            middlewareConfiguration = new NullStreamProducerMiddlewareConfiguration();
        }

        ArgumentNullException.ThrowIfNull(middlewareConfiguration);

        var configuration = (TConfiguration)middlewareConfiguration;

        var ctx = new DefaultStreamProducerMiddlewareContext<TRequest, TItem, TConfiguration>(
            request,
            next,
            configuration,
            serviceProvider,
            conquerorContext,
            cancellationToken
        );

        if (typeof(TConfiguration) == typeof(NullStreamProducerMiddlewareConfiguration))
        {
            var middleware = (IStreamProducerMiddleware)serviceProvider.GetRequiredService(typeof(TMiddleware));

            return middleware.Execute(ctx);
        }

        var middlewareWithConfiguration =
            (IStreamProducerMiddleware<TConfiguration>)serviceProvider.GetRequiredService(typeof(TMiddleware));

        return middlewareWithConfiguration.Execute(ctx);
    }
}

internal sealed record NullStreamProducerMiddlewareConfiguration;
