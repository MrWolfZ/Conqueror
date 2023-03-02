using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests;

public abstract class TestBase : IDisposable
{
    private readonly WebApplicationFactory<Program> applicationFactory;
    private readonly ServiceProvider clientServices;
    private readonly HttpClient httpTestClient;

    protected TestBase()
    {
        applicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // configure tests to produce logs; this is very useful to debug failing tests
            builder.ConfigureLogging(o => o.ClearProviders()
                                           .AddSimpleConsole()
                                           .SetMinimumLevel(LogLevel.Information));
        });

        httpTestClient = applicationFactory.CreateClient();

        // create a dedicated service provider for resolving command and query clients
        // to prevent interference with other services from the actual application; we also
        // configure the client services to use our test server's HTTP client
        clientServices = new ServiceCollection().AddConquerorCQSHttpClientServices(o => o.UseHttpClient(httpTestClient))
                                                .BuildServiceProvider();
    }

    public void Dispose()
    {
        clientServices.Dispose();
        httpTestClient.Dispose();
        applicationFactory.Dispose();
    }

    protected T ResolveOnServer<T>()
        where T : notnull => applicationFactory.Services.GetRequiredService<T>();

    protected T ResolveOnClient<T>()
        where T : notnull => clientServices.GetRequiredService<T>();
}
