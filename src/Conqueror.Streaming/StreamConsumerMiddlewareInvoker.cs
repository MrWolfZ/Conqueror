namespace Conqueror.Streaming;

internal sealed class StreamConsumerMiddlewareInvoker<TMiddleware, TConfiguration> : IStreamConsumerMiddlewareInvoker
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
    public Task Invoke<TItem>(
        TItem item,
        StreamConsumerMiddlewareNext<TItem> next,
        object? middlewareConfiguration,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    )
    {
        if (typeof(TConfiguration) == typeof(NullStreamConsumerMiddlewareConfiguration))
        {
            middlewareConfiguration = new NullStreamConsumerMiddlewareConfiguration();
        }

        ArgumentNullException.ThrowIfNull(middlewareConfiguration);

        var configuration = (TConfiguration)middlewareConfiguration;

        var ctx = new DefaultStreamConsumerMiddlewareContext<TItem, TConfiguration>(
            item,
            next,
            configuration,
            serviceProvider,
            conquerorContext,
            cancellationToken
        );

        if (typeof(TConfiguration) == typeof(NullStreamConsumerMiddlewareConfiguration))
        {
            var middleware = (IStreamConsumerMiddleware)serviceProvider.GetRequiredService(typeof(TMiddleware));

            return middleware.Execute(ctx);
        }

        var middlewareWithConfiguration =
            (IStreamConsumerMiddleware<TConfiguration>)serviceProvider.GetRequiredService(typeof(TMiddleware));

        return middlewareWithConfiguration.Execute(ctx);
    }
}

internal sealed record NullStreamConsumerMiddlewareConfiguration;
