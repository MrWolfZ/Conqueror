using System.Threading;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IFileSystemSignalReceivers
{
    ReceiverExecutionHandle RunReceivers(ISignalReceivers receivers, CancellationToken cancellationToken);

    ReceiverExecutionHandle RunReceiver<THandler>(ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IFileSystemSignalHandler, ISignalHandlerWithSourceGeneration;
}
