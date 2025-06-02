namespace Examples.BlazorWebAssembly.UI;

public sealed partial class ChatEntryBroadcastedHandler : ChatEntryBroadcasted.IHandler
{
    public event EventHandler<ChatEntryBroadcasted>? OnSignal;

    static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline) => pipeline.UseLogging();

    public Task Handle(ChatEntryBroadcasted signal, CancellationToken cancellationToken = default)
    {
        OnSignal?.Invoke(this, signal);
        return Task.CompletedTask;
    }

    static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
    {
        _ = receiver.Enable(new(receiver.ServiceProvider.GetApiBaseAddress(), "api/signals/sse"));
    }
}
