using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalHandlerRegistry
{
    IReadOnlyCollection<ISignalReceiverHandlerInvoker<TTypesInjector>> GetReceiverHandlerInvokers<TTypesInjector>()
        where TTypesInjector : class, ISignalHandlerTypesInjector;
}
