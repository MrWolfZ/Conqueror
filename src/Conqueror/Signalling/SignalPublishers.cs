using System;
using System.Diagnostics.CodeAnalysis;

namespace Conqueror.Signalling;

internal sealed class SignalPublishers(IServiceProvider serviceProvider) : ISignalPublishers
{
    public THandler For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        THandler>()
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>
    {
        return TSignal.DefaultTypeInjector.CreateWithSignalTypes(new Injectable<THandler>(serviceProvider));
    }

    public THandler For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        THandler>(SignalTypes<TSignal, THandler> signalTypes)
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>
    {
        return TSignal.DefaultTypeInjector.CreateWithSignalTypes(new Injectable<THandler>(serviceProvider));
    }

    private sealed class Injectable<THandlerParam>(IServiceProvider serviceProvider) : IDefaultSignalTypesInjectable<THandlerParam>
        where THandlerParam : class
    {
        THandlerParam IDefaultSignalTypesInjectable<THandlerParam>
            .WithInjectedTypes<
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                TSignal,
                TGeneratedHandlerInterface,
                TGeneratedHandlerAdapter>()
        {
            var dispatcher = new SignalDispatcher<TSignal>(serviceProvider,
                                                           new(b => b.UseInProcessWithSequentialBroadcastingStrategy()),
                                                           null,
                                                           SignalTransportRole.Publisher);

            var adapter = new TGeneratedHandlerAdapter
            {
                Dispatcher = dispatcher,
            };

            return adapter as THandlerParam ?? throw new InvalidOperationException("could not create handler adapter");
        }
    }
}
