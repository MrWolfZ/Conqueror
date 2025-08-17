namespace Examples.BlazorWebAssembly.API.Chat;

using System.Collections.Concurrent;

internal sealed class ChatRepository
{
    private readonly ConcurrentQueue<ChatEntry> entries = [];

    public async Task Add(ChatEntry entry)
    {
        await Task.Yield();
        entries.Enqueue(entry);
    }

    public async Task<IReadOnlyCollection<ChatEntry>> GetEntries()
    {
        await Task.Yield();

        return entries;
    }
}
