namespace Conqueror.Recipes.Messaging.TestingHttp.Tests;

internal sealed class TestHost : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> applicationFactory = new();
    private readonly ServiceProvider clientServices;

    private TestHost()
    {
        HttpTestClient = applicationFactory.CreateClient();

        // create a dedicated service provider for resolving message senders to prevent
        // interference with the services of the actual application
        clientServices = new ServiceCollection().AddConquerorHttpClient().BuildServiceProvider();
    }

    public HttpClient HttpTestClient { get; }

    public IMessageSenders MessageSenders => clientServices.GetRequiredService<IMessageSenders>();

    public static TestHost Create() => new();

    public T ResolveOnServer<T>()
        where T : notnull => applicationFactory.Services.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await clientServices.DisposeAsync();
        HttpTestClient.Dispose();
        await applicationFactory.DisposeAsync();
    }
}
