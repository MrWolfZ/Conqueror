using System;

namespace Conqueror.Signalling;

internal sealed class SignalPublishers(
    IServiceProvider serviceProvider,
    IConquerorContextAccessor conquerorContextAccessor,
    ISignalIdFactory signalIdFactory)
    : ISignalPublishers
{
    public TIHandler For<TSignal, TIHandler>(SignalTypes<TSignal, TIHandler> signalTypes)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return TSignal.CoreTypesInjector.Create(new Injectable<TIHandler>(serviceProvider, conquerorContextAccessor, signalIdFactory));
    }

    private readonly struct Injectable<TIHandlerParam>(
        IServiceProvider serviceProvider,
        IConquerorContextAccessor conquerorContextAccessor,
        ISignalIdFactory signalIdFactory) : ICoreSignalHandlerTypesInjectable<TIHandlerParam>
        where TIHandlerParam : class
    {
        TIHandlerParam ICoreSignalHandlerTypesInjectable<TIHandlerParam>.WithInjectedTypes<TSignal, TIHandler, TProxy, THandler>()
        {
            var dispatcher = new SignalDispatcher<TSignal>(
                serviceProvider,
                conquerorContextAccessor,
                signalIdFactory,
                new(static b => b.UseInProcessWithSequentialBroadcastingStrategy()),
                null,
                SignalTransportRole.Publisher,
                null);

            var proxy = new TProxy
            {
                Dispatcher = dispatcher,
            };

            return proxy as TIHandlerParam ?? throw new InvalidOperationException("could not create handler proxy");
        }
    }
}
