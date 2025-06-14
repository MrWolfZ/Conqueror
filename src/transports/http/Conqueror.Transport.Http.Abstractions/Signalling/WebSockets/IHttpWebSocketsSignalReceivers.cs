using System.Threading;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpWebSocketsSignalReceivers
{
    SignalReceiverExecutionHandle RunReceivers(ISignalReceivers receivers, CancellationToken cancellationToken);

    SignalReceiverExecutionHandle RunReceiver<THandler>(ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpWebSocketsSignalHandler, ISignalHandlerWithSourceGeneration;
}
