namespace Conqueror;

public readonly record struct MessageMiddlewareContext<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    private readonly State state;

    public MessageMiddlewareContext(
        List<IMessageMiddleware<TMessage, TResponse>> middlewares,
        IMessageSender<TMessage, TResponse> sender,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        MessageTransportType transportType
    )
    {
        Debug.Assert(middlewares.Count > 0, "this should only be called if there are middlewares to execute");

        state = new State
        {
            Middlewares = middlewares,
            Sender = sender,
            ServiceProvider = serviceProvider,
            ConquerorContext = conquerorContext,
            TransportType = transportType,
        };
    }

    public required TMessage Message { get; init; }

    public bool HasUnitResponse => typeof(TResponse) == typeof(UnitMessageResponse);

    public required CancellationToken CancellationToken { get; init; }

    public IServiceProvider ServiceProvider => state.ServiceProvider;

    public ConquerorContext ConquerorContext => state.ConquerorContext;

    public MessageTransportType TransportType => state.TransportType;

    private int CurrentIndex { get; init; }

    public Task<TResponse> Next(TMessage message, CancellationToken cancellationToken)
    {
        var nextIndex = CurrentIndex + 1;
        if (nextIndex < state.Middlewares.Count)
        {
            var updatedContext = this with
            {
                Message = message,
                CancellationToken = cancellationToken,
                CurrentIndex = nextIndex,
            };

            return state.Middlewares[nextIndex].Execute(updatedContext);
        }

        return state.Sender.Send(message, ServiceProvider, ConquerorContext, cancellationToken);
    }

    // performance optimization: we capture the immutable parts of the context in a separate
    // record so that it only needs to be allocated once per pipeline execution instead of
    // being embedded in the context, which would require a lot of copying of fields, especially
    // when the context might be captured in the async state machine of a middleware's Execute
    // method
    private sealed record State
    {
        public required List<IMessageMiddleware<TMessage, TResponse>> Middlewares { get; init; }

        public required IMessageSender<TMessage, TResponse> Sender { get; init; }

        public required IServiceProvider ServiceProvider { get; init; }

        public required ConquerorContext ConquerorContext { get; init; }

        public required MessageTransportType TransportType { get; init; }
    }
}
