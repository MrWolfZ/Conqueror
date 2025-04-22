using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Signalling;

internal sealed class SignalTransportRegistry(IEnumerable<SignalHandlerRegistration> registrations)
    : ISignalTransportRegistry
{
    private readonly List<SignalHandlerRegistration> allRegistrations = registrations.ToList();
    private readonly ConcurrentDictionary<Type, List<ISignalReceiverHandlerInvoker>> invokersByInjectorType = new();
    private readonly ConcurrentDictionary<Type, ISignalTypesInjector?> typesInjectorsBySignalType = new();

    public TTypesInjector? GetTypesInjectorForSignalType<TTypesInjector>(Type signalType)
        where TTypesInjector : class, ISignalTypesInjector
    {
        return (TTypesInjector?)typesInjectorsBySignalType.GetOrAdd(signalType,
                                                                    t => allRegistrations.FirstOrDefault(r => r.SignalType == t)?
                                                                                         .TypeInjectors
                                                                                         .OfType<TTypesInjector>()
                                                                                         .FirstOrDefault());
    }

    public IReadOnlyCollection<ISignalReceiverHandlerInvoker<TTypesInjector>> GetSignalInvokersForReceiver<TTypesInjector>()
        where TTypesInjector : class, ISignalTypesInjector
    {
        var entries = invokersByInjectorType.GetOrAdd(typeof(TTypesInjector),
                                                      _ => [..PopulateSignalInvokersForReceiver<TTypesInjector>()]);

        return entries.OfType<ISignalReceiverHandlerInvoker<TTypesInjector>>().ToList();
    }

    private List<ISignalReceiverHandlerInvoker> PopulateSignalInvokersForReceiver<TTypesInjector>()
        where TTypesInjector : class, ISignalTypesInjector
    {
        var invokers = from r in allRegistrations
                       let typesInjector = r.TypeInjectors.OfType<TTypesInjector>().FirstOrDefault()
                       where typesInjector is not null
                       select (ISignalReceiverHandlerInvoker)new SignalReceiverHandlerInvoker<TTypesInjector>(r, typesInjector);

        return invokers.ToList();
    }
}

internal sealed record SignalHandlerRegistration(
    Type SignalType,
    Type? HandlerType,
    Delegate? HandlerFn,
    ISignalHandlerInvoker Invoker,
    IReadOnlyCollection<ISignalTypesInjector> TypeInjectors);
