using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceiversRunner(IServiceProvider serviceProvider)
{
    private static readonly ConfigurationInjectable ConfigInjectable = new();

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, the source is returned to the caller")]
    public HttpSseSignalReceiver? ConfigureReceiver(Type handlerType, Action<IHttpSseSignalReceiver> configureReceiver)
    {
        try
        {
            var receiver = new HttpSseSignalReceiver(serviceProvider, handlerType);
            configureReceiver(receiver);

            if (!receiver.IsEnabled)
            {
                return null;
            }

            foreach (var invoker in serviceProvider.GetRequiredService<ISignalHandlerRegistry>()
                                                   .GetReceiverHandlerInvokers<IHttpSseSignalHandlerTypesInjector>()
                                                   .Where(i => i.HandlerType == receiver.HandlerType))
            {
                _ = invoker.TypesInjector.Inject(ConfigInjectable, new(invoker, receiver));
            }

            return receiver;
        }
        catch (Exception ex)
        {
            throw new SignalReceiverRunFailedException($"failed to run the signal receiver for handler type '{handlerType}'", ex)
            {
                HandlerType = handlerType,
                SignalTransportType = new(ServersSentEventsTransportName, SignalTransportRole.Receiver),
            };
        }
    }

    internal SignalReceiverRun Run(HttpSseSignalReceiver receiver, CancellationToken cancellationToken)
    {
        try
        {
            var runner = new HttpSseSignalReceiverRunner(
                receiver,
                serviceProvider.GetRequiredService<IConquerorContextAccessor>());

            return runner.Run(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new SignalReceiverRunFailedException($"failed to run the signal receiver for handler type '{receiver.HandlerType}'", ex)
            {
                HandlerType = receiver.HandlerType,
                SignalTransportType = new(ServersSentEventsTransportName, SignalTransportRole.Receiver),
            };
        }
    }

    private readonly record struct ConfigurationInjectableArg(ISignalReceiverHandlerInvoker Invoker, HttpSseSignalReceiver Receiver);

    private sealed class ConfigurationInjectable : IHttpSseSignalTypesInjectable<ConfigurationInjectableArg, object?>
    {
        object? IHttpSseSignalTypesInjectable<ConfigurationInjectableArg, object?>
            .WithInjectedTypes<TSignal, TIHandler>(ConfigurationInjectableArg arg)
        {
            arg.Receiver.AddSignalType<TSignal>(arg.Invoker);

            return null;
        }
    }
}
