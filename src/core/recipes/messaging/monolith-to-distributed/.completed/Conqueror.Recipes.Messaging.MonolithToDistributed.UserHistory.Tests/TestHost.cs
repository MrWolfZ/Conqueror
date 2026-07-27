namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Tests;

using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.EntryPoint.WebApi;

internal sealed class TestHost : IAsyncDisposable
{
    // bootstrap the UserHistory web application so that the tests exercise it
    // just like in production
    private readonly WebApplicationFactory<UserHistoryProgram> applicationFactory = new();

    // dedicated service provider for sending messages to the web application from the
    // outside, to prevent interference with the services of the server application
    private readonly ServiceProvider clientServices = new ServiceCollection().AddConquerorHttpClient().BuildServiceProvider();

    private TestHost()
    {
        HttpClient = applicationFactory.CreateClient();
    }

    // messages are invoked in-process through the same public API the application uses
    public IMessageSenders MessageSenders => applicationFactory.Services.GetRequiredService<IMessageSenders>();

    // senders resolved from here send messages through the web app's HTTP API, just like
    // a remote caller (e.g. the Counters web app) would in production
    public IMessageSenders HttpMessageSenders => clientServices.GetRequiredService<IMessageSenders>();

    public HttpClient HttpClient { get; }

    public static TestHost Create() => new();

    public T ResolveOnServer<T>()
        where T : notnull => applicationFactory.Services.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await clientServices.DisposeAsync();
        HttpClient.Dispose();
        await applicationFactory.DisposeAsync();
    }
}
