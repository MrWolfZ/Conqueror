using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Signalling;

internal sealed class InProcessSignalReceiver(IServiceProvider serviceProviderField)
{
    private readonly Lazy<List<Receiver>> allActiveReceivers = new(
        () => GetActiveReceivers(serviceProviderField),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<Delegate>> receiversBySignalType = new();

    public async Task Broadcast<TSignal>(
        TSignal signal,
        IServiceProvider serviceProvider,
        ISignalBroadcastingStrategy broadcastingStrategy,
        CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>
    {
        var fns = receiversBySignalType.GetOrAdd(signal.GetType(), GetSignalHandlerFnsForSignalType<TSignal>);

        await broadcastingStrategy.BroadcastSignal(
                                      (IReadOnlyCollection<SignalHandlerFn<TSignal>>)fns,
                                      serviceProvider,
                                      signal,
                                      cancellationToken)
                                  .ConfigureAwait(false);
    }

    /// <summary>
    ///     <c>TSignal</c> is the type of the signal that the handler observes, while
    ///     <c>signalType</c> is the type of the concrete signal that is being broadcasted,
    ///     which may be a sub-type of <c>TSignal</c>.
    /// </summary>
    private List<SignalHandlerFn<TSignal>> GetSignalHandlerFnsForSignalType<TSignal>(Type signalType)
        where TSignal : class, ISignal<TSignal>
    {
        return allActiveReceivers.Value
                                 .SelectMany(r => r.GetSignalHandlerFns<TSignal>(signalType))
                                 .ToList();
    }

    private static List<Receiver> GetActiveReceivers(IServiceProvider serviceProvider)
    {
        var receiverByHandlerType = new Dictionary<Type, Receiver>();

        return serviceProvider.GetRequiredService<SignalHandlerRegistry>()
                              .GetReceiverHandlerInvokers<ICoreSignalHandlerTypesInjector>()
                              .Select(i => i.TypesInjector.Create(new Injectable(serviceProvider, i, receiverByHandlerType)))
                              .OfType<Receiver>()
                              .Distinct()
                              .ToList();
    }

    private sealed class Injectable(
        IServiceProvider serviceProvider,
        ISignalReceiverHandlerInvoker invoker,
        Dictionary<Type, Receiver> receiverByHandlerType)
        : ICoreSignalHandlerTypesInjectable<Receiver?>
    {
        Receiver? ICoreSignalHandlerTypesInjectable<Receiver?>.WithInjectedTypes<TSignal, TIHandler, TProxy, THandler>()
        {
            if (!receiverByHandlerType.TryGetValue(typeof(THandler), out var receiver))
            {
                receiverByHandlerType.Add(typeof(THandler), receiver = new(serviceProvider));
                THandler.ConfigureInProcessReceiver(receiver);
            }

            if (!receiver.IsEnabled)
            {
                return null;
            }

            receiver.AddInvoker(invoker);

            return receiver;
        }
    }

    private sealed class Receiver(IServiceProvider serviceProvider) : IInProcessSignalReceiver
    {
        private readonly List<ISignalReceiverHandlerInvoker> invokers = [];

        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public bool IsEnabled { get; private set; } = true;

        public void Disable() => IsEnabled = false;

        public void AddInvoker(ISignalReceiverHandlerInvoker invoker)
        {
            invokers.Add(invoker);
        }

        public List<SignalHandlerFn<TSignal>> GetSignalHandlerFns<TSignal>(Type signalType)
            where TSignal : class, ISignal<TSignal>
        {
            return invokers.Where(i => i.SignalType.IsAssignableFrom(signalType))
                           .Select(CreateHandlerFn)
                           .ToList();

            SignalHandlerFn<TSignal> CreateHandlerFn(ISignalReceiverHandlerInvoker invoker)
                => (signal, serviceProvider, cancellationToken) => invoker.Invoke(
                    signal,
                    serviceProvider,
                    ConquerorConstants.InProcessTransportName,
                    cancellationToken);
        }
    }
}
