using System.Threading;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpSseSignalReceivers
{
    ReceiverExecutionHandle RunReceivers(ISignalReceivers receivers, CancellationToken cancellationToken);

    ReceiverExecutionHandle RunReceiver<THandler>(ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpSseSignalHandler, ISignalHandlerWithSourceGeneration;
}
