namespace Conqueror.Messaging;

internal sealed class MessageSenders(IServiceProvider serviceProvider, IMessageDispatcher dispatcher) : IMessageSenders
{
    private static readonly Injectable HandlerCreationInjectable = new();

    public TIHandler For<TMessage, TResponse, TIHandler>(MessageTypes<TMessage, TResponse, TIHandler> messageTypes)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        var proxy = ((ICoreMessageHandlerTypesInjector)TMessage.CoreTypesInjector).Inject(
            HandlerCreationInjectable,
            new(serviceProvider, dispatcher)
        );

        Debug.Assert(
            proxy is TIHandler,
            $"handler proxy was not of correct type; expected handler type '{typeof(TIHandler)}', actual '{proxy.GetType()}'"
        );

        return (TIHandler)proxy;
    }

    private readonly record struct InjectableArg(IServiceProvider ServiceProvider, IMessageDispatcher Dispatcher);

    private sealed class Injectable : ICoreMessageHandlerTypesInjectable<InjectableArg, object>
    {
        object ICoreMessageHandlerTypesInjectable<InjectableArg, object>.WithInjectedTypes<
            TMessage,
            TResponse,
            TIHandler,
            TProxy,
            TIPipeline,
            TPipelineProxy
        >(InjectableArg arg)
        {
            return new TProxy
            {
                ServiceProvider = arg.ServiceProvider,
                Dispatcher = arg.Dispatcher,
                Pipeline = new MessagePipeline<TMessage, TResponse>(handlerType: null, arg.ServiceProvider),
            };
        }
    }
}
