using System;
using System.Collections.Generic;

namespace Conqueror.Transport.Http.Client.Signalling.WebSockets;

internal sealed class HttpWebSocketsSignalReceiverFactory(IServiceProvider serviceProvider)
    : ISignalReceiverFactory<IHttpWebSocketsSignalHandlerTypesInjector, HttpWebSocketsSignalReceiver>
{
    private static readonly ConfigurationInjectable ConfigInjectable = new();

    public string TransportTypeName => ServersSentEventsTransportName;

    public HttpWebSocketsSignalReceiver? CreateReceiverForHandlerType(
        Type? handlerType,
        IReadOnlyCollection<ISignalReceiverHandlerInvoker<IHttpWebSocketsSignalHandlerTypesInjector>> invokers,
        IHttpWebSocketsSignalHandlerTypesInjector typesInjector)
    {
        var receiver = new HttpWebSocketsSignalReceiver(serviceProvider, handlerType);
        typesInjector.ConfigureHttpWebSocketsReceiver(receiver);

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

    private readonly record struct ConfigurationInjectableArg(ISignalReceiverHandlerInvoker Invoker, HttpWebSocketsSignalReceiver Receiver);

    private sealed class ConfigurationInjectable : IHttpWebSocketsSignalTypesInjectable<ConfigurationInjectableArg, object?>
    {
        object? IHttpWebSocketsSignalTypesInjectable<ConfigurationInjectableArg, object?>
            .WithInjectedTypes<TSignal, TIHandler>(ConfigurationInjectableArg arg)
        {
            arg.Receiver.AddSignalType<TSignal>(arg.Invoker);

            return null;
        }
    }
}
