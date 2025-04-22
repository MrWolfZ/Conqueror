using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class InProcessSignalReceiver(SignalTransportRegistry registry, IServiceProvider serviceProviderField)
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<ISignalReceiverHandlerInvoker<IDefaultSignalTypesInjector>>> invokersBySignalType = new();

    public async Task Broadcast<TSignal>(TSignal signal,
                                         IServiceProvider serviceProvider,
                                         ISignalBroadcastingStrategy broadcastingStrategy,
                                         string transportTypeName,
                                         CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>
    {
        var relevantInvokers = invokersBySignalType.GetOrAdd(signal.GetType(), GetSignalInvokers);

        await broadcastingStrategy.BroadcastSignal(relevantInvokers, serviceProvider, signal, transportTypeName, cancellationToken)
                                  .ConfigureAwait(false);
    }

    private IReadOnlyCollection<ISignalReceiverHandlerInvoker<IDefaultSignalTypesInjector>> GetSignalInvokers(Type signalType)
    {
        return registry.GetSignalInvokersForReceiver<IDefaultSignalTypesInjector>()
                       .Where(i => signalType.IsAssignableTo(i.SignalType))
                       .Where(i => i.TypesInjector.CreateWithSignalTypes(new Injectable(serviceProviderField)))
                       .ToList();
    }

    private sealed class Injectable(IServiceProvider serviceProvider) : IDefaultSignalTypesInjectable<bool>
    {
        bool IDefaultSignalTypesInjectable<bool>.WithInjectedTypes<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            TSignal,
            TGeneratedHandlerInterface,
            TGeneratedHandlerAdapter>()
        {
            // delegate handlers are always active
            return true;
        }

        bool IDefaultSignalTypesInjectable<bool>.WithInjectedTypes<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            TSignal,
            TGeneratedHandlerInterface,
            TGeneratedHandlerAdapter,
            THandler>()
        {
            var receiver = new Receiver<TSignal>(serviceProvider);
            THandler.ConfigureInProcessReceiver(receiver);
            return receiver.IsEnabled;
        }
    }

    private sealed class Receiver<TSignal>(IServiceProvider serviceProvider) : IInProcessSignalReceiver<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public bool IsEnabled { get; private set; }

        public void Enable() => IsEnabled = true;

        public void Disable() => IsEnabled = false;
    }
}
