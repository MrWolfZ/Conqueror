namespace Examples.BlazorWebAssembly.API.Chat;

internal sealed partial class GetChatHandler(ChatRepository repository) : GetChat.IHandler
{
    public static void ConfigurePipeline(GetChat.IPipeline pipeline)
        => pipeline.UseDefault()
                   .ConfigureTimeout(TimeSpan.FromSeconds(10))
                   .RequirePermission(nameof(GetChat));

    public async Task<ChatEntry[]> Handle(GetChat message, CancellationToken cancellationToken = default)
    {
        return (await repository.GetEntries()).ToArray();
    }
}
