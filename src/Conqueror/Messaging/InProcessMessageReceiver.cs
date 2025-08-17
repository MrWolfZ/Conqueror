namespace Conqueror.Messaging;

internal sealed class InProcessMessageReceiver<TMessage, TResponse>(IServiceProvider serviceProvider)
    : IInProcessMessageReceiver
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public bool MustBeConfiguredOnEveryMessage { get; private set; }
    public Type MessageType { get; } = typeof(TMessage);

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public bool IsEnabled { get; private set; } = true;

    public void Disable() => IsEnabled = false;

    public IInProcessMessageReceiver ConfigureOnEveryMessage()
    {
        MustBeConfiguredOnEveryMessage = true;

        return this;
    }

    public IInProcessMessageReceiver Enable()
    {
        IsEnabled = true;

        return this;
    }
}
