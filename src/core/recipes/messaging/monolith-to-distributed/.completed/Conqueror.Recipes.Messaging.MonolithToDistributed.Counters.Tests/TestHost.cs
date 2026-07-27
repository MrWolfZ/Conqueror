namespace Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Tests;

using Conqueror.Recipes.Messaging.MonolithToDistributed.Core.Application;
using Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.EntryPoint.WebApi;
using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Contracts;
using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.EntryPoint.WebApi;
using Microsoft.AspNetCore.Hosting;

internal sealed class TestHost : IAsyncDisposable
{
    // bootstrap both web applications so that the tests exercise the Counters context
    // with full integration with the UserHistory context, just like in production
    private readonly WebApplicationFactory<UserHistoryProgram> userHistoryApp = new();
    private readonly WebApplicationFactory<CountersProgram> countersAppFactory = new();
    private readonly WebApplicationFactory<CountersProgram> countersApp;
    private readonly HttpClient userHistoryHttpClient;

    private TestHost()
    {
        userHistoryHttpClient = userHistoryApp.CreateClient();

        countersApp = countersAppFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                // replace the sender for the UserHistory context's message so that it is
                // sent to the in-memory test server instead of the configured base address
                services.AddSingleton<SetMostRecentlyIncrementedCounterForUser.IHandler>(
                    p => p.GetRequiredService<IMessageSenders>()
                          .For(SetMostRecentlyIncrementedCounterForUser.T)
                          .WithPipeline(pipeline => pipeline.UseDefault())
                          .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(userHistoryHttpClient)))));
    }

    // messages are invoked in-process through the same public API the application uses
    public IMessageSenders MessageSenders => countersApp.Services.GetRequiredService<IMessageSenders>();

    // the UserHistory context's messages are invoked in-process on the UserHistory web app
    public IMessageSenders UserHistoryMessageSenders => userHistoryApp.Services.GetRequiredService<IMessageSenders>();

    public static TestHost Create() => new();

    public T ResolveOnServer<T>()
        where T : notnull => countersApp.Services.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await countersApp.DisposeAsync();
        await countersAppFactory.DisposeAsync();
        userHistoryHttpClient.Dispose();
        await userHistoryApp.DisposeAsync();
    }
}
