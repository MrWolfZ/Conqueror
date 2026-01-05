namespace Conqueror;

public interface IIteratorServers
{
    IServiceProvider ServiceProvider { get; }

    ReceiverExecutionHandle RunServers<TTypesInjector, TServer>(
        IIteratorServerFactory<TTypesInjector, TServer> serverFactory,
        IIteratorServerRunner<TServer> serverRunner,
        CancellationToken cancellationToken
    )
        where TTypesInjector : class, IIteratorHandlerTypesInjector
        where TServer : class;

    ReceiverExecutionHandle RunServer<THandler, TTypesInjector, TServer>(
        IIteratorServerFactory<TTypesInjector, TServer> serverFactory,
        IIteratorServerRunner<TServer> serverRunner,
        CancellationToken cancellationToken
    )
        where TTypesInjector : class, IIteratorHandlerTypesInjector
        where TServer : class;

    ReceiverExecutionHandle CombineExecutions(IReadOnlyCollection<ReceiverExecutionHandle> executionHandles);
}
