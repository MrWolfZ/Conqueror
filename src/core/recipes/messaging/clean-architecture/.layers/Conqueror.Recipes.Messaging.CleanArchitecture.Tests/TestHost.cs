namespace Conqueror.Recipes.Messaging.CleanArchitecture.Tests;

internal sealed class TestHost : IAsyncDisposable
{
    // bootstrap the whole application; this makes the tests exercise the full
    // integration of all bounded contexts, just like in production
    private readonly WebApplicationFactory<Program> applicationFactory = new();

    private TestHost()
    {
    }

    // messages are invoked in-process through the same public API the application uses
    public IMessageSenders MessageSenders => applicationFactory.Services.GetRequiredService<IMessageSenders>();

    public static TestHost Create() => new();

    public T ResolveOnServer<T>()
        where T : notnull => applicationFactory.Services.GetRequiredService<T>();

    public ValueTask DisposeAsync() => applicationFactory.DisposeAsync();
}
