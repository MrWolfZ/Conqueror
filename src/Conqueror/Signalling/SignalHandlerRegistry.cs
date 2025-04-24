using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Signalling;

internal sealed class SignalHandlerRegistry(IEnumerable<SignalHandlerRegistration> registrations)
    : ISignalHandlerRegistry
{
    private readonly List<SignalHandlerRegistration> allRegistrations = registrations.ToList();
    private readonly ConcurrentDictionary<Type, List<ISignalReceiverHandlerInvoker>> invokersByInjectorType = new();

    public IReadOnlyCollection<ISignalReceiverHandlerInvoker<TTypesInjector>> GetReceiverHandlerInvokers<TTypesInjector>()
        where TTypesInjector : class, ISignalHandlerTypesInjector
    {
        return invokersByInjectorType.GetOrAdd(typeof(TTypesInjector),
                                               _ => [..PopulateSignalInvokersForReceiver<TTypesInjector>()])
                                     .OfType<ISignalReceiverHandlerInvoker<TTypesInjector>>()
                                     .ToList();
    }

    private List<ISignalReceiverHandlerInvoker> PopulateSignalInvokersForReceiver<TTypesInjector>()
        where TTypesInjector : class, ISignalHandlerTypesInjector
    {
        var invokers = from r in allRegistrations
                       let typesInjector = r.TypeInjectors.OfType<TTypesInjector>().FirstOrDefault(i => i.SignalType == r.SignalType)
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
    IReadOnlyCollection<ISignalHandlerTypesInjector> TypeInjectors);
