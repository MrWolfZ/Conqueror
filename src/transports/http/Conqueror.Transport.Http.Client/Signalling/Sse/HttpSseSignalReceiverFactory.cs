namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceiverFactory(IServiceProvider serviceProvider)
    : ISignalReceiverFactory<IHttpSseSignalHandlerTypesInjector, HttpSseSignalReceiver>
{
    private static readonly ConfigurationInjectable ConfigInjectable = new();

    public string TransportTypeName => ServersSentEventsTransportName;

    public HttpSseSignalReceiver? CreateReceiverForHandlerType(
        Type? handlerType,
        IReadOnlyCollection<ISignalReceiverHandlerInvoker<IHttpSseSignalHandlerTypesInjector>> invokers,
        IHttpSseSignalHandlerTypesInjector typesInjector
    )
    {
        var receiver = new HttpSseSignalReceiver(
            serviceProvider,
            invokers.Select(i => i.SignalType).ToArray(),
            handlerType
        );
        typesInjector.ConfigureHttpSseReceiver(receiver);

        if (!receiver.IsEnabled)
        {
            return null;
        }

        foreach (var invoker in invokers)
        {
            _ = invoker.TypesInjector.Inject(ConfigInjectable, new(invoker, receiver));
        }

        return receiver;
    }

    private readonly record struct ConfigurationInjectableArg(
        ISignalReceiverHandlerInvoker Invoker,
        HttpSseSignalReceiver Receiver
    );

    private sealed class ConfigurationInjectable : IHttpSseSignalTypesInjectable<ConfigurationInjectableArg, object?>
    {
        object? IHttpSseSignalTypesInjectable<ConfigurationInjectableArg, object?>.WithInjectedTypes<
            TSignal,
            TIHandler
        >(ConfigurationInjectableArg arg)
        {
            arg.Receiver.AddSignalType<TSignal>(arg.Invoker);

            return null;
        }
    }
}
