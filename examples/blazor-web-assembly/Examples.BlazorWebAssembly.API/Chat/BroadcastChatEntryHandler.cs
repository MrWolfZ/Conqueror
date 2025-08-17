namespace Examples.BlazorWebAssembly.API.Chat;

internal sealed partial class BroadcastChatEntryHandler(
    ChatRepository repository,
    ISignalPublishers signalPublishers)
    : BroadcastChatEntry.IHandler
{
    public static void ConfigurePipeline(BroadcastChatEntry.IPipeline pipeline)

        // to showcase how the default pipeline can be configured per handler
        => pipeline.UseDefault()
            .ConfigureTimeout(TimeSpan.FromSeconds(value: 10))
            .RequirePermission(nameof(BroadcastChatEntry))
            .ConfigureRetry(maxNumberOfAttempts: 2, TimeSpan.FromSeconds(value: 2))
            .OutsideOfAmbientTransaction();

    public async Task Handle(BroadcastChatEntry message, CancellationToken cancellationToken = default)
    {
        var timestamp = SystemTime.Now;
        await repository.Add(
            new() { User = message.User, Content = message.Content, Timestamp = timestamp });

        await signalPublishers.For(ChatEntryBroadcasted.T)
            .WithDefaultPublisherPipeline(typeof(BroadcastChatEntryHandler))
            .WithTransport(b => b.UseHttpWebSockets())
            .Handle(
                new() { User = message.User, Content = message.Content, Timestamp = timestamp },
                cancellationToken);
    }
}
