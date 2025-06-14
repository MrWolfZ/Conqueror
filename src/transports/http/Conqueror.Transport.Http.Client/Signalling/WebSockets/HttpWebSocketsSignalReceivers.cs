using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Client.Signalling.WebSockets;

internal sealed class HttpWebSocketsSignalReceivers : IHttpWebSocketsSignalReceivers
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive, the source is returned to the caller")]
    public ReceiverExecutionHandle RunReceivers(ISignalReceivers receivers, CancellationToken cancellationToken)
    {
        var registry = receivers.ServiceProvider.GetRequiredService<ISignalHandlerRegistry>();
        var invokers = registry.GetReceiverHandlerInvokers<IHttpWebSocketsSignalHandlerTypesInjector>();

        var configuredHandlerTypes = new HashSet<Type>();

        var configuredReceivers = invokers.Select(i =>
                                          {
                                              // for delegate handlers
                                              if (i.HandlerType is null)
                                              {
                                                  return ConfigureHttpWebSocketsSignalReceiver(
                                                      receivers,
                                                      i,
                                                      i.TypesInjector.ConfigureHttpWebSocketsReceiver);
                                              }

                                              if (!configuredHandlerTypes.Add(i.HandlerType))
                                              {
                                                  // if the receiver was already configured, we can skip it
                                                  return null;
                                              }

                                              return ConfigureHttpWebSocketsSignalReceiver(
                                                  receivers,
                                                  i.HandlerType,
                                                  i.TypesInjector.ConfigureHttpWebSocketsReceiver);
                                          })
                                          .OfType<HttpWebSocketsSignalReceiver>()
                                          .ToList();

        return receivers.CombineExecutions(configuredReceivers.Select(r => RunHttpWebSocketsSignalReceiver(receivers, r, cancellationToken)).ToList());
    }

    public ReceiverExecutionHandle RunReceiver<THandler>(ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpWebSocketsSignalHandler, ISignalHandlerWithSourceGeneration
    {
        var receiver = ConfigureHttpWebSocketsSignalReceiver<THandler>(receivers);

        if (receiver is null)
        {
            return new(
                Task.CompletedTask,
                Task.CompletedTask,
                cancellationTokenSource: null,
                onDispose: null);
        }

        return RunHttpWebSocketsSignalReceiver(receivers, receiver, cancellationToken);
    }

    private static HttpWebSocketsSignalReceiver? ConfigureHttpWebSocketsSignalReceiver<THandler>(ISignalReceivers receivers)
        where THandler : class, IHttpWebSocketsSignalHandler
    {
        return ConfigureHttpWebSocketsSignalReceiver(receivers, typeof(THandler), THandler.ConfigureHttpWebSocketsReceiver);
    }

    private static HttpWebSocketsSignalReceiver? ConfigureHttpWebSocketsSignalReceiver(
        ISignalReceivers receivers,
        Type handlerType,
        Action<IHttpWebSocketsSignalReceiver> configureReceiver)
    {
        return receivers.ServiceProvider
                        .GetRequiredService<HttpWebSocketsSignalReceiversRunner>()
                        .ConfigureReceiver(handlerType, configureReceiver);
    }

    private static HttpWebSocketsSignalReceiver? ConfigureHttpWebSocketsSignalReceiver(
        ISignalReceivers receivers,
        ISignalReceiverHandlerInvoker<IHttpWebSocketsSignalHandlerTypesInjector> receiverHandlerInvoker,
        Action<IHttpWebSocketsSignalReceiver> configureReceiver)
    {
        return receivers.ServiceProvider
                        .GetRequiredService<HttpWebSocketsSignalReceiversRunner>()
                        .ConfigureReceiver(receiverHandlerInvoker, configureReceiver);
    }

    private static ReceiverExecutionHandle RunHttpWebSocketsSignalReceiver(
        ISignalReceivers receivers,
        HttpWebSocketsSignalReceiver receiver,
        CancellationToken cancellationToken)
    {
        return receivers.ServiceProvider
                        .GetRequiredService<HttpWebSocketsSignalReceiversRunner>()
                        .Run(receiver, cancellationToken);
    }
}
