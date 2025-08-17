namespace Conqueror.Messaging;

internal sealed class MessageHandlerRegistry(
    IServiceProvider serviceProvider,
    IEnumerable<MessageHandlerRegistration> registrations
) : IMessageHandlerRegistry
{
    private readonly ConcurrentDictionary<
        (Type MessageType, Type InjectorType),
        IMessageReceiverHandlerInvoker?
    > invokerByMessageAndInjectorType = [];

    private readonly ConcurrentDictionary<Type, List<IMessageReceiverHandlerInvoker>> invokersByInjectorType = [];

    private readonly Dictionary<Type, MessageHandlerRegistration> registrationByMessageType =
        registrations.ToDictionary(r => r.MessageType);

    public IMessageReceiverHandlerInvoker<TTypesInjector>? GetReceiverHandlerInvoker<
        TMessage,
        TResponse,
        TTypesInjector
    >()
        where TMessage : class, IMessage<TMessage, TResponse>
        where TTypesInjector : class, IMessageHandlerTypesInjector
    {
        var key = (typeof(TMessage), typeof(TTypesInjector));

        return invokerByMessageAndInjectorType.GetOrAdd(
                key,
                static (_, @this) => @this.GetInvokerForMessageAndInjectorType<TMessage, TTypesInjector>(),
                this
            ) as IMessageReceiverHandlerInvoker<TTypesInjector>;
    }

    public IReadOnlyCollection<
        IMessageReceiverHandlerInvoker<TTypesInjector>
    > GetReceiverHandlerInvokers<TTypesInjector>()
        where TTypesInjector : class, IMessageHandlerTypesInjector
    {
        return invokersByInjectorType
            .GetOrAdd(
                typeof(TTypesInjector),
                static (_, @this) => [.. @this.PopulateMessageInvokersForReceiver<TTypesInjector>()],
                this
            )
            .Cast<IMessageReceiverHandlerInvoker<TTypesInjector>>()
            .ToList();
    }

    private List<IMessageReceiverHandlerInvoker> PopulateMessageInvokersForReceiver<TTypesInjector>()
        where TTypesInjector : class, IMessageHandlerTypesInjector
    {
        var invokers =
            from r in registrationByMessageType.Values
            let typesInjector = r
                .TypeInjectors.OfType<TTypesInjector>()
                .FirstOrDefault(i => i.MessageType == r.MessageType)
            where typesInjector is not null
            let handlerInvoker = r.HandlerInvokerFactory(serviceProvider)
            select (IMessageReceiverHandlerInvoker)
                new MessageReceiverHandlerInvoker<TTypesInjector>(r, handlerInvoker, typesInjector);

        return invokers.ToList();
    }

    private MessageReceiverHandlerInvoker<TTypesInjector>? GetInvokerForMessageAndInjectorType<
        TMessage,
        TTypesInjector
    >()
        where TTypesInjector : class, IMessageHandlerTypesInjector
    {
        var registration = registrationByMessageType.GetValueOrDefault(typeof(TMessage));

        if (registration is null)
        {
            return null;
        }

        var typesInjector = registration
            .TypeInjectors.OfType<TTypesInjector>()
            .FirstOrDefault(i => i.MessageType == registration.MessageType);
        var handlerInvoker = registration.HandlerInvokerFactory(serviceProvider);

        return typesInjector is null
            ? null
            : new MessageReceiverHandlerInvoker<TTypesInjector>(registration, handlerInvoker, typesInjector);
    }
}

internal sealed record MessageHandlerRegistration(
    Type MessageType,
    Type ResponseType,
    Type? HandlerType,
    Delegate? HandlerFn,
    Func<IServiceProvider, IMessageHandlerInvoker> HandlerInvokerFactory,
    IReadOnlyCollection<IMessageHandlerTypesInjector> TypeInjectors
);
