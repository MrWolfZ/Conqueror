using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalTransportRegistry
{
    TTypesInjector? GetTypesInjectorForSignalType<TTypesInjector>(Type signalType)
        where TTypesInjector : class, ISignalTypesInjector;

    IReadOnlyCollection<ISignalReceiverHandlerInvoker<TTypesInjector>> GetSignalInvokersForReceiver<TTypesInjector>()
        where TTypesInjector : class, ISignalTypesInjector;
}
