namespace Conqueror;

public interface IMessageReceiverRunner<in TReceiver>
    where TReceiver : class
{
    ReceiverExecutionHandle RunReceiver(TReceiver receiver, CancellationToken cancellationToken);
}
