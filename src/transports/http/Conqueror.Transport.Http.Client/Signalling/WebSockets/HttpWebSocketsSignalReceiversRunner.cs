using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Client.Signalling.WebSockets;

internal sealed class HttpWebSocketsSignalReceiversRunner(IServiceProvider serviceProvider)
{
    private static readonly ConfigurationInjectable ConfigInjectable = new();

    public HttpWebSocketsSignalReceiver? ConfigureReceiver(Type handlerType, Action<IHttpWebSocketsSignalReceiver> configureReceiver)
    {
        try
        {
            var receiver = new HttpWebSocketsSignalReceiver(serviceProvider, handlerType);
            configureReceiver(receiver);

            if (!receiver.IsEnabled)
            {
                return null;
            }

            foreach (var invoker in serviceProvider.GetRequiredService<ISignalHandlerRegistry>()
                                                   .GetReceiverHandlerInvokers<IHttpWebSocketsSignalHandlerTypesInjector>()
                                                   .Where(i => i.HandlerType == receiver.HandlerType))
            {
                _ = invoker.TypesInjector.Inject(ConfigInjectable, new(invoker, receiver));
            }

            return receiver;
        }
        catch (Exception ex)
        {
            throw new ReceiverExecutionFailedException($"failed to run the signal receiver for handler type '{handlerType}'", ex)
            {
                HandlerType = handlerType,
                SignalTransportType = new(WebSocketsTransportName, SignalTransportRole.Receiver),
            };
        }
    }

    public HttpWebSocketsSignalReceiver? ConfigureReceiver(
        ISignalReceiverHandlerInvoker<IHttpWebSocketsSignalHandlerTypesInjector> receiverHandlerInvoker,
        Action<IHttpWebSocketsSignalReceiver> configureReceiver)
    {
        try
        {
            var receiver = new HttpWebSocketsSignalReceiver(serviceProvider, receiverHandlerInvoker.HandlerType);
            configureReceiver(receiver);

            if (!receiver.IsEnabled)
            {
                return null;
            }

            _ = receiverHandlerInvoker.TypesInjector.Inject(ConfigInjectable, new(receiverHandlerInvoker, receiver));

            return receiver;
        }
        catch (Exception ex)
        {
            throw new ReceiverExecutionFailedException($"failed to run the signal receiver for handler type '{receiverHandlerInvoker.HandlerType}'", ex)
            {
                HandlerType = receiverHandlerInvoker.HandlerType,
                SignalTransportType = new(WebSocketsTransportName, SignalTransportRole.Receiver),
            };
        }
    }

    internal ReceiverExecutionHandle Run(HttpWebSocketsSignalReceiver receiver, CancellationToken cancellationToken)
    {
        try
        {
            var runner = new HttpWebSocketsSignalReceiverRunner(
                receiver,
                serviceProvider.GetRequiredService<IConquerorContextAccessor>());

            return runner.Run(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new ReceiverExecutionFailedException($"failed to run the signal receiver for handler type '{receiver.HandlerType}'", ex)
            {
                HandlerType = receiver.HandlerType,
                SignalTransportType = new(WebSocketsTransportName, SignalTransportRole.Receiver),
            };
        }
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
