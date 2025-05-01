namespace Examples.BlazorWebAssembly.Tests;

[TestFixture]
internal sealed class ChatTests
{
    [Test]
    public async Task GivenEmptyChat_WhenGettingChat_ReturnsEmptyChat()
    {
        await using var host = TestHost.Create();

        var handler = host.CreateMessageHttpSender(GetChat.T);

        var entries = await handler.Handle(new(), host.TimeoutToken);

        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task GivenEmptyChat_WhenBroadcastingChatEntryAndGettingChat_ReturnsBroadcastedChatEntry()
    {
        await using var host = TestHost.Create();

        var broadcastHandler = host.CreateMessageHttpSender(BroadcastChatEntry.T);
        var getHandler = host.CreateMessageHttpSender(GetChat.T);

        await broadcastHandler.Handle(new() { User = "User", Content = "Content" }, host.TimeoutToken);

        var entries = await getHandler.Handle(new(), host.TimeoutToken);

        Assert.That(entries, Is.EqualTo(new[] { new ChatEntry { User = "User", Content = "Content", Timestamp = host.CurrentTime } }));
    }
}
