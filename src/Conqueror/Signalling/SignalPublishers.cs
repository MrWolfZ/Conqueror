using System;

namespace Conqueror.Signalling;

internal sealed class SignalPublishers(
    IServiceProvider serviceProvider,
    IConquerorContextAccessor conquerorContextAccessor,
    ISignalIdFactory signalIdFactory)
    : ISignalPublishers
{
    private static readonly Injectable HandlerCreationInjectable = new();

    public TIHandler For<TSignal, TIHandler>(SignalTypes<TSignal, TIHandler> signalTypes)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return ((ICoreSignalHandlerTypesInjector)TSignal.CoreTypesInjector).Inject(
            HandlerCreationInjectable,
            new(serviceProvider,
                conquerorContextAccessor,
                signalIdFactory)) as TIHandler ?? throw new InvalidOperationException("could not create handler proxy");
    }

    private readonly record struct InjectableArg(
        IServiceProvider ServiceProvider,
        IConquerorContextAccessor ConquerorContextAccessor,
        ISignalIdFactory SignalIdFactory);

    private sealed class Injectable : ICoreSignalHandlerTypesInjectable<InjectableArg, object>
    {
        object ICoreSignalHandlerTypesInjectable<InjectableArg, object>.WithInjectedTypes<TSignal, TIHandler, TProxy>(InjectableArg arg)
        {
            var dispatcher = new SignalDispatcher<TSignal>(
                arg.ServiceProvider,
                arg.ConquerorContextAccessor,
                arg.SignalIdFactory,
                new(static b => b.UseInProcessWithSequentialBroadcastingStrategy()),
                null,
                SignalTransportRole.Publisher,
                null);

            return new TProxy
            {
                Dispatcher = dispatcher,
            };
        }
    }
}
