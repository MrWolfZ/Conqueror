namespace Conqueror;

public interface IIteratorServerRunner<in TServer>
    where TServer : class
{
    ReceiverExecutionHandle RunServer(TServer server, CancellationToken cancellationToken);
}
