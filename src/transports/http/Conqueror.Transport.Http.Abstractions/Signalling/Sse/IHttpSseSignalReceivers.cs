using System.Threading;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpSseSignalReceivers
{
    SignalReceiverRun RunReceivers(ISignalReceivers receivers, CancellationToken cancellationToken);

    SignalReceiverRun RunReceiver<THandler>(ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpSseSignalHandler;
}
