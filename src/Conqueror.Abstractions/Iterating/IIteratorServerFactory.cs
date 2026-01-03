namespace Conqueror;

public interface IIteratorServerFactory<in TTypesInjector, out TServer>
    where TTypesInjector : class, IIteratorHandlerTypesInjector
    where TServer : class
{
    string TransportTypeName { get; }

    TServer? CreateServerForHandlerType(
        Type? handlerType,
        IReadOnlyCollection<IIteratorServerHandlerInvoker<TTypesInjector>> invokers,
        TTypesInjector typesInjector
    );
}
