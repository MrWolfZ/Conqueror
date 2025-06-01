using System;
using System.Diagnostics;

namespace Conqueror.Signalling;

internal sealed class SignalPublishers(
    IServiceProvider serviceProvider,
    ISignalDispatcher dispatcher)
    : ISignalPublishers
{
    private static readonly Injectable HandlerCreationInjectable = new();

    public TIHandler For<TSignal, TIHandler>(SignalTypes<TSignal, TIHandler> signalTypes)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        var proxy = ((ICoreSignalHandlerTypesInjector)TSignal.CoreTypesInjector).Inject(
            HandlerCreationInjectable,
            new(serviceProvider, dispatcher));

        Debug.Assert(proxy is TIHandler, $"handler proxy was not of correct type; expected handler type '{typeof(TIHandler)}', actual '{proxy.GetType()}'");

        return (TIHandler)proxy;
    }

    private readonly record struct InjectableArg(
        IServiceProvider ServiceProvider,
        ISignalDispatcher Dispatcher);

    private sealed class Injectable : ICoreSignalHandlerTypesInjectable<InjectableArg, object>
    {
        object ICoreSignalHandlerTypesInjectable<InjectableArg, object>.WithInjectedTypes<TSignal, TIHandler, TProxy>(InjectableArg arg)
        {
            return new TProxy
            {
                ServiceProvider = arg.ServiceProvider,
                Dispatcher = arg.Dispatcher,
            };
        }
    }
}
