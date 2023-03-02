using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.EntryPoint.WebApi;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Contracts;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.EntryPoint.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Tests;

public abstract class TestBase : IDisposable
{
    private readonly ServiceProvider clientServices;
    private readonly WebApplicationFactory<CountersProgram> countersApp;
    private readonly HttpClient httpTestClient;
    private readonly WebApplicationFactory<UserHistoryProgram> userHistoryApp;
    private readonly HttpClient userHistoryClient;

    protected TestBase()
    {
        // bootstrap the UserHistory web app to allow calling its commands and queries during tests
        userHistoryApp = new WebApplicationFactory<UserHistoryProgram>().WithWebHostBuilder(builder =>
        {
            // configure tests to produce logs; this is very useful to debug failing tests
            builder.ConfigureLogging(o => o.ClearProviders()
                                           .AddSimpleConsole()
                                           .SetMinimumLevel(LogLevel.Information));
        });

        userHistoryClient = userHistoryApp.CreateClient();

        countersApp = new WebApplicationFactory<CountersProgram>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(o => o.ClearProviders()
                                           .AddSimpleConsole()
                                           .SetMinimumLevel(LogLevel.Information));

            builder.ConfigureServices(services =>
            {
                services.ConfigureConquerorCQSHttpClientOptions(o =>
                {
                    // configure Conqueror.CQS to use the UserHistory web app's test HTTP client for the single
                    // command that we need
                    o.UseHttpClientForCommand<SetMostRecentlyIncrementedCounterForUserCommand>(userHistoryClient);
                });
            });
        });

        httpTestClient = countersApp.CreateClient();

        // create a dedicated service provider for resolving command and query clients
        // to prevent interference with other services from the server application
        clientServices = new ServiceCollection().AddConquerorCQSHttpClientServices(
                                                    o =>
                                                        // use the test HTTP client for the Counters app for all command and query clients
                                                        o.UseHttpClient(httpTestClient)

                                                         // in our tests we want to call a command from the UserHistory app as part of our test
                                                         // assertions; therefore we configure our
                                                         .UseHttpClientForQuery<GetMostRecentlyIncrementedCounterForUserQuery>(userHistoryClient))
                                                .BuildServiceProvider();
    }

    public void Dispose()
    {
        clientServices.Dispose();
        httpTestClient.Dispose();
        userHistoryClient.Dispose();
        countersApp.Dispose();
        userHistoryApp.Dispose();
    }

    protected T ResolveOnServer<T>()
        where T : notnull => countersApp.Services.GetRequiredService<T>();

    protected T CreateCommandClient<T>()
        where T : class, ICommandHandler => clientServices.GetRequiredService<ICommandClientFactory>().CreateCommandClient<T>(b => b.UseHttp(new("http://localhost")));

    protected T CreateQueryClient<T>()
        where T : class, IQueryHandler => clientServices.GetRequiredService<IQueryClientFactory>().CreateQueryClient<T>(b => b.UseHttp(new("http://localhost")));
}
