using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
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

        var injectable = new Injectable(receivers, cancellationToken);

        return receivers.CombineRuns(invokers.Select(i => i.TypesInjector.Create(injectable)).OfType<SignalReceiverRun>().ToList());
    }

    public static SignalReceiverRun RunHttpSseSignalReceiver<THandler>(this ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpSseSignalHandler
    {
        var singletons = receivers.ServiceProvider.GetRequiredService<ConquerorSingletons>();
        var runner = singletons.GetOrAddSingleton(p => new HttpSseSignalReceiversRunner(p));

        return runner.Run<THandler>(cancellationToken);
    }

    private sealed class Injectable(
        ISignalReceivers receivers,
        CancellationToken cancellationToken)
        : IHttpSseSignalTypesInjectable<SignalReceiverRun?>
    {
        private readonly HashSet<Type> processedHandlerTypes = new();

        SignalReceiverRun? IHttpSseSignalTypesInjectable<SignalReceiverRun?>.WithInjectedTypes<TSignal, TIHandler, THandler>()
        {
            if (!processedHandlerTypes.Add(typeof(THandler)))
            {
                return null;
            }

            return receivers.RunHttpSseSignalReceiver<THandler>(cancellationToken);
        }
    }
}
