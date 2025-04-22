using System;

namespace Conqueror.Signalling;

internal sealed class SignalPublishers(IServiceProvider serviceProvider) : ISignalPublishers
{
    public TIHandler For<TSignal, TIHandler>()
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return TIHandler.CoreTypesInjector.Create(new Injectable<TIHandler>(serviceProvider));
    }

    public TIHandler For<TSignal, TIHandler>(SignalTypes<TSignal, TIHandler> signalTypes)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return TIHandler.CoreTypesInjector.Create(new Injectable<TIHandler>(serviceProvider));
    }

    private sealed class Injectable<THandlerParam>(IServiceProvider serviceProvider) : ICoreSignalHandlerTypesInjectable<THandlerParam>
        where THandlerParam : class
    {
        THandlerParam ICoreSignalHandlerTypesInjectable<THandlerParam>.WithInjectedTypes<TSignal, TIHandler, TProxy, THandler>()
        {
            var dispatcher = new SignalDispatcher<TSignal>(serviceProvider,
                                                           new(b => b.UseInProcessWithSequentialBroadcastingStrategy()),
                                                           null,
                                                           SignalTransportRole.Publisher);

            var adapter = new TProxy
            {
                Dispatcher = dispatcher,
            };

            return adapter as THandlerParam ?? throw new InvalidOperationException("could not create handler adapter");
        }
    }
}
