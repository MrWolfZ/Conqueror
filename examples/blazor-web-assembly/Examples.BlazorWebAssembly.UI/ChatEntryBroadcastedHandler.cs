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

    static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
    {
        var apiBaseAddress = receiver.ServiceProvider.GetApiBaseAddress();

        var uri = new UriBuilder
        {
            Scheme = "wss",
            Host = apiBaseAddress.Host,
            Port = apiBaseAddress.Port,
            Path = "api/signals/ws",
        }.Uri;

        var attempt = 0;

        _ = receiver.Enable(uri)

                    // exponential back-off
                    .WithReconnectDelayFunction(async (
                                                    _,
                                                    _,
                                                    _,
                                                    token) =>
                                                {
                                                    var delay = TimeSpan.FromSeconds(Math.Min(60, 2 ^ attempt));

                                                    await Task.Delay(delay, token);

                                                    attempt += 1;
                                                });
    }
}
