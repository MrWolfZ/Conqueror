namespace Conqueror;

public interface ISignalHandlerRegistry
{
    IReadOnlyCollection<ISignalReceiverHandlerInvoker<TTypesInjector>> GetReceiverHandlerInvokers<TTypesInjector>()
        where TTypesInjector : class, ISignalHandlerTypesInjector;
}
