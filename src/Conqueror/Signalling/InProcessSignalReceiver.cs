using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class InProcessSignalReceiver(SignalHandlerRegistry registry, IServiceProvider serviceProviderField)
{
    private readonly ConcurrentDictionary<Type, List<IInvoker>> invokersBySignalType = new();

    public async Task Broadcast<TSignal>(TSignal signal,
                                         IServiceProvider serviceProvider,
                                         ISignalBroadcastingStrategy broadcastingStrategy,
                                         CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>
    {
        var relevantInvokers = invokersBySignalType.GetOrAdd(signal.GetType(), GetSignalInvokers);

        await broadcastingStrategy.BroadcastSignal(relevantInvokers.ConvertAll<SignalHandlerFn<TSignal>>(i => i.Invoke),
                                                   serviceProvider,
                                                   signal,
                                                   cancellationToken)
                                  .ConfigureAwait(false);
    }

    private List<IInvoker> GetSignalInvokers(Type signalType)
    {
        return registry.GetReceiverHandlerInvokers<ICoreSignalHandlerTypesInjector>()
                       .Where(i => signalType.IsAssignableTo(i.TypesInjector.SignalType))
                       .Select(i => i.TypesInjector.Create(new Injectable(i, serviceProviderField)))
                       .OfType<IInvoker>()
                       .ToList();
    }

    private interface IInvoker
    {
        Task Invoke(object signal, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class Invoker<TSignal>(ISignalReceiverHandlerInvoker invoker) : IInvoker
        where TSignal : class, ISignal<TSignal>
    {
        public Task Invoke(object signal, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            return invoker.Invoke((TSignal)signal,
                                  serviceProvider,
                                  ConquerorConstants.InProcessTransportName,
                                  cancellationToken);
        }
    }

    private sealed class Injectable(ISignalReceiverHandlerInvoker invoker, IServiceProvider serviceProvider) : ICoreSignalHandlerTypesInjectable<IInvoker?>
    {
        IInvoker? ICoreSignalHandlerTypesInjectable<IInvoker?>.WithInjectedTypes<TSignal, TIHandler, TProxy, THandler>()
        {
            var receiver = new Receiver<TSignal>(serviceProvider);
            THandler.ConfigureInProcessReceiver(receiver);
            return !receiver.IsEnabled ? null : new Invoker<TSignal>(invoker);
        }
    }

    private sealed class Receiver<TSignal>(IServiceProvider serviceProvider) : IInProcessSignalReceiver
        where TSignal : class, ISignal<TSignal>
    {
        public Type SignalType { get; } = typeof(TSignal);

        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public bool IsEnabled { get; private set; } = true;

        public IInProcessSignalReceiver Disable()
        {
            IsEnabled = false;
            return this;
        }
    }
}
