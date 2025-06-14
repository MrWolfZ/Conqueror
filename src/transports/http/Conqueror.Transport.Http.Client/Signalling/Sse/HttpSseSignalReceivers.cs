using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceivers : IHttpSseSignalReceivers
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive, the source is returned to the caller")]
    public ReceiverExecutionHandle RunReceivers(ISignalReceivers receivers, CancellationToken cancellationToken)
    {
        var registry = receivers.ServiceProvider.GetRequiredService<ISignalHandlerRegistry>();
        var invokers = registry.GetReceiverHandlerInvokers<IHttpSseSignalHandlerTypesInjector>();

        var configuredHandlerTypes = new HashSet<Type>();

        var configuredReceivers = invokers.Select(i =>
                                          {
                                              // for delegate handlers
                                              if (i.HandlerType is null)
                                              {
                                                  return ConfigureHttpSseSignalReceiver(
                                                      receivers,
                                                      i,
                                                      i.TypesInjector.ConfigureHttpSseReceiver);
                                              }

                                              if (!configuredHandlerTypes.Add(i.HandlerType))
                                              {
                                                  // if the receiver was already configured, we can skip it
                                                  return null;
                                              }

                                              return ConfigureHttpSseSignalReceiver(receivers, i.HandlerType, i.TypesInjector.ConfigureHttpSseReceiver);
                                          })
                                          .OfType<HttpSseSignalReceiver>()
                                          .ToList();

        return receivers.CombineExecutions(configuredReceivers.Select(r => RunHttpSseSignalReceiver(receivers, r, cancellationToken)).ToList());
    }

    public ReceiverExecutionHandle RunReceiver<THandler>(ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpSseSignalHandler, ISignalHandlerWithSourceGeneration
    {
        var receiver = ConfigureHttpSseSignalReceiver<THandler>(receivers);

        if (receiver is null)
        {
            return new(Task.CompletedTask, Task.CompletedTask, cancellationTokenSource: null, onDispose: null);
        }

        return RunHttpSseSignalReceiver(receivers, receiver, cancellationToken);
    }

    private static HttpSseSignalReceiver? ConfigureHttpSseSignalReceiver<THandler>(ISignalReceivers receivers)
        where THandler : class, IHttpSseSignalHandler
    {
        return ConfigureHttpSseSignalReceiver(receivers, typeof(THandler), THandler.ConfigureHttpSseReceiver);
    }

    private static HttpSseSignalReceiver? ConfigureHttpSseSignalReceiver(
        ISignalReceivers receivers,
        Type handlerType,
        Action<IHttpSseSignalReceiver> configureReceiver)
    {
        return receivers.ServiceProvider
                        .GetRequiredService<HttpSseSignalReceiversRunner>()
                        .ConfigureReceiver(handlerType, configureReceiver);
    }

    private static HttpSseSignalReceiver? ConfigureHttpSseSignalReceiver(
        ISignalReceivers receivers,
        ISignalReceiverHandlerInvoker<IHttpSseSignalHandlerTypesInjector> receiverHandlerInvoker,
        Action<IHttpSseSignalReceiver> configureReceiver)
    {
        return receivers.ServiceProvider
                        .GetRequiredService<HttpSseSignalReceiversRunner>()
                        .ConfigureReceiver(receiverHandlerInvoker, configureReceiver);
    }

    private static ReceiverExecutionHandle RunHttpSseSignalReceiver(
        ISignalReceivers receivers,
        HttpSseSignalReceiver receiver,
        CancellationToken cancellationToken)
    {
        return receivers.ServiceProvider
                        .GetRequiredService<HttpSseSignalReceiversRunner>()
                        .Run(receiver, cancellationToken);
    }
}
