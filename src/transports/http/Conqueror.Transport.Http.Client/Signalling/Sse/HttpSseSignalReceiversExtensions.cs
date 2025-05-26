using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Transport.Http.Client.Signalling.Sse;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpSseSignalReceiversExtensions
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive, the source is returned to the caller")]
    public static SignalReceiverRun RunHttpSseSignalReceivers(this ISignalReceivers receivers, CancellationToken cancellationToken)
    {
        var registry = receivers.ServiceProvider.GetRequiredService<ISignalHandlerRegistry>();
        var invokers = registry.GetReceiverHandlerInvokers<IHttpSseSignalHandlerTypesInjector>();

        var configuredHandlerTypes = new HashSet<Type>();

        var configuredReceivers = invokers.Select(i =>
                                          {
                                              if (i.HandlerType is null || !configuredHandlerTypes.Add(i.HandlerType))
                                              {
                                                  // if the receiver was already configured, we can skip it
                                                  return null;
                                              }

                                              return receivers.ConfigureHttpSseSignalReceiver(i.HandlerType!, i.TypesInjector.ConfigureHttpSseReceiver);
                                          })
                                          .OfType<HttpSseSignalReceiver>()
                                          .ToList();

        return receivers.CombineRuns(configuredReceivers.Select(r => receivers.RunHttpSseSignalReceiver(r, cancellationToken)).ToList());
    }

    public static SignalReceiverRun RunHttpSseSignalReceiver<THandler>(this ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpSseSignalHandler
    {
        var receiver = receivers.ConfigureHttpSseSignalReceiver<THandler>();

        if (receiver is null)
        {
            return new(Task.CompletedTask, Task.CompletedTask, null);
        }

        return receivers.RunHttpSseSignalReceiver(receiver, cancellationToken);
    }

    private static HttpSseSignalReceiver? ConfigureHttpSseSignalReceiver<THandler>(this ISignalReceivers receivers)
        where THandler : class, IHttpSseSignalHandler
    {
        return receivers.ConfigureHttpSseSignalReceiver(typeof(THandler), THandler.ConfigureHttpSseReceiver);
    }

    private static HttpSseSignalReceiver? ConfigureHttpSseSignalReceiver(
        this ISignalReceivers receivers,
        Type handlerType,
        Action<IHttpSseSignalReceiver> configureReceiver)
    {
        var singletons = receivers.ServiceProvider.GetRequiredService<ConquerorSingletons>();
        var runner = singletons.GetOrAddSingleton(p => new HttpSseSignalReceiversRunner(p));

        return runner.ConfigureReceiver(handlerType, configureReceiver);
    }

    private static SignalReceiverRun RunHttpSseSignalReceiver(
        this ISignalReceivers receivers,
        HttpSseSignalReceiver receiver,
        CancellationToken cancellationToken)
    {
        var singletons = receivers.ServiceProvider.GetRequiredService<ConquerorSingletons>();
        var runner = singletons.GetOrAddSingleton(p => new HttpSseSignalReceiversRunner(p));

        return runner.Run(receiver, cancellationToken);
    }
}
