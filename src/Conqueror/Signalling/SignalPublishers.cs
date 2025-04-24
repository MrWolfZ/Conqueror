using System;

namespace Conqueror.Signalling;

internal sealed class SignalPublishers(IServiceProvider serviceProvider) : ISignalPublishers
{
    public TIHandler For<TSignal, TIHandler>(SignalTypes<TSignal, TIHandler> signalTypes)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return TIHandler.CoreTypesInjector.Create(new Injectable<TIHandler>(serviceProvider));
    }

    private sealed class Injectable<TIHandlerParam>(IServiceProvider serviceProvider) : ICoreSignalHandlerTypesInjectable<TIHandlerParam>
        where TIHandlerParam : class
    {
        TIHandlerParam ICoreSignalHandlerTypesInjectable<TIHandlerParam>.WithInjectedTypes<TSignal, TIHandler, TProxy, THandler>()
        {
            var dispatcher = new SignalDispatcher<TSignal>(serviceProvider,
                                                           new(b => b.UseInProcessWithSequentialBroadcastingStrategy()),
                                                           null,
                                                           SignalTransportRole.Publisher);

            var proxy = new TProxy
            {
                Dispatcher = dispatcher,
            };

            return proxy as TIHandlerParam ?? throw new InvalidOperationException("could not create handler proxy");
        }
    }
}
