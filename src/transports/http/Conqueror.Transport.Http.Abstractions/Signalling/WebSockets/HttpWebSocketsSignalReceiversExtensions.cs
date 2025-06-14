using System;
using System.Threading;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpWebSocketsSignalReceiversExtensions
{
    public static SignalReceiverExecutionHandle RunHttpWebSocketsSignalReceivers(this ISignalReceivers receivers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receivers);

        if (receivers.ServiceProvider.GetService(typeof(IHttpWebSocketsSignalReceivers)) is not IHttpWebSocketsSignalReceivers httpReceivers)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IHttpWebSocketsSignalReceivers)}'; did you forget to add the Conqueror HTTP client to the service collection?");
        }

        return httpReceivers.RunReceivers(receivers, cancellationToken);
    }

    public static SignalReceiverExecutionHandle RunHttpWebSocketsSignalReceiver<THandler>(this ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpWebSocketsSignalHandler, ISignalHandlerWithSourceGeneration
    {
        ArgumentNullException.ThrowIfNull(receivers);

        if (receivers.ServiceProvider.GetService(typeof(IHttpWebSocketsSignalReceivers)) is not IHttpWebSocketsSignalReceivers httpReceivers)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IHttpWebSocketsSignalReceivers)}'; did you forget to add the Conqueror HTTP client to the service collection?");
        }

        return httpReceivers.RunReceiver<THandler>(receivers, cancellationToken);
    }
}
