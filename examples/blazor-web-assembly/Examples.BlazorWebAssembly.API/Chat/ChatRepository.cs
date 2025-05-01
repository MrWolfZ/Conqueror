using System.Collections.Concurrent;

namespace Examples.BlazorWebAssembly.API.Chat;

internal sealed class ChatRepository
{
    private readonly ConcurrentQueue<ChatEntry> entries = new();

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
