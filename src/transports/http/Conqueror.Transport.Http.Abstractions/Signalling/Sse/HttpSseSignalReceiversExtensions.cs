using System;
using System.Threading;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpSseSignalReceiversExtensions
{
    public static SignalReceiverExecutionHandle RunHttpSseSignalReceivers(this ISignalReceivers receivers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receivers);

        if (receivers.ServiceProvider.GetService(typeof(IHttpSseSignalReceivers)) is not IHttpSseSignalReceivers httpReceivers)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IHttpSseSignalReceivers)}'; did you forget to add the Conqueror HTTP client to the service collection?");
        }

        return httpReceivers.RunReceivers(receivers, cancellationToken);
    }

    public static SignalReceiverExecutionHandle RunHttpSseSignalReceiver<THandler>(this ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpSseSignalHandler, ISignalHandlerWithSourceGeneration
    {
        ArgumentNullException.ThrowIfNull(receivers);

        if (receivers.ServiceProvider.GetService(typeof(IHttpSseSignalReceivers)) is not IHttpSseSignalReceivers httpReceivers)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IHttpSseSignalReceivers)}'; did you forget to add the Conqueror HTTP client to the service collection?");
        }

        return httpReceivers.RunReceiver<THandler>(receivers, cancellationToken);
    }
}
