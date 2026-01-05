namespace Conqueror;

public delegate Task<TResponse> MessageHandlerFn<in TMessage, TResponse>(
    TMessage message,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken
)
    where TMessage : class, IMessage<TMessage, TResponse>;

public delegate Task MessageHandlerFn<in TMessage>(
    TMessage message,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken
)
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>;

public delegate TResponse MessageHandlerSyncFn<in TMessage, out TResponse>(
    TMessage message,
    IServiceProvider serviceProvider
)
    where TMessage : class, IMessage<TMessage, TResponse>;

public delegate void MessageHandlerSyncFn<in TMessage>(TMessage message, IServiceProvider serviceProvider)
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>;
