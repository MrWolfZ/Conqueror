using System.Threading;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalReceiverRunner<in TReceiver>
    where TReceiver : class
{
    ReceiverExecutionHandle RunReceiver(
        TReceiver receiver,
        CancellationToken cancellationToken);
}
