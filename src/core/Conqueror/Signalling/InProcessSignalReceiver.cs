namespace Conqueror.Signalling;

internal sealed class InProcessSignalReceiver(IServiceProvider serviceProviderField)
{
    private readonly Lazy<List<Receiver>> allActiveReceivers = new(
        () => GetActiveReceivers(serviceProviderField),
        LazyThreadSafetyMode.ExecutionAndPublication
    );

    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<Delegate>> receiversBySignalType = [];

    public Task Broadcast<TSignal>(
        TSignal signal,
        IServiceProvider serviceProvider,
        ISignalBroadcastingStrategy broadcastingStrategy,
        CancellationToken cancellationToken
    )
        where TSignal : class, ISignal<TSignal>
    {
        var fns = receiversBySignalType.GetOrAdd(signal.GetType(), GetSignalHandlerFnsForSignalType<TSignal>);

        return broadcastingStrategy.BroadcastSignal(
            (IReadOnlyCollection<SignalHandlerFn<TSignal>>)fns,
            serviceProvider,
            signal,
            cancellationToken
        );
    }

    /// <summary>
    ///     <c>TSignal</c> is the type of the signal that the handler observes, while
    ///     <c>signalType</c> is the type of the concrete signal that is being broadcasted,
    ///     which may be a sub-type of <c>TSignal</c>.
    /// </summary>
    /// <typeparam name="TSignal">the signal type to observe</typeparam>
    /// <param name="signalType">the signal type being broadcast</param>
    private List<SignalHandlerFn<TSignal>> GetSignalHandlerFnsForSignalType<TSignal>(Type signalType)
        where TSignal : class, ISignal<TSignal> =>
        allActiveReceivers.Value.SelectMany(r => r.GetSignalHandlerFns<TSignal>(signalType)).ToList();

    private static List<Receiver> GetActiveReceivers(IServiceProvider serviceProvider)
    {
        var receiverByHandlerType = new Dictionary<Type, Receiver>();

        return serviceProvider
            .GetRequiredService<SignalHandlerRegistry>()
            .GetReceiverHandlerInvokers<ICoreSignalHandlerTypesInjector>()
            .Select(ConfigureReceiver)
            .OfType<Receiver>()
            .Distinct()
            .ToList();

        Receiver? ConfigureReceiver(ISignalReceiverHandlerInvoker<ICoreSignalHandlerTypesInjector> invoker)
        {
            // delegate handlers are always enabled
            if (invoker.HandlerType is null)
            {
                var r = new Receiver(serviceProvider);
                r.AddInvoker(invoker);

                return r;
            }

            if (!receiverByHandlerType.TryGetValue(invoker.HandlerType, out var receiver))
            {
                receiver = new Receiver(serviceProvider);
                receiverByHandlerType.Add(invoker.HandlerType, receiver);
                invoker.TypesInjector.ConfigureInProcessReceiver(receiver);
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

        public void AddInvoker(ISignalReceiverHandlerInvoker invoker) => invokers.Add(invoker);

        public List<SignalHandlerFn<TSignal>> GetSignalHandlerFns<TSignal>(Type signalType)
            where TSignal : class, ISignal<TSignal>
        {
            return invokers.Where(i => i.SignalType.IsAssignableFrom(signalType)).Select(CreateHandlerFn).ToList();

            SignalHandlerFn<TSignal> CreateHandlerFn(ISignalReceiverHandlerInvoker invoker)
            {
                return (signal, serviceProvider, cancellationToken) =>
                    invoker.Invoke(
                        signal,
                        serviceProvider,
                        ConquerorConstants.InProcessTransportName,
                        cancellationToken
                    );
            }
        }
    }
}
