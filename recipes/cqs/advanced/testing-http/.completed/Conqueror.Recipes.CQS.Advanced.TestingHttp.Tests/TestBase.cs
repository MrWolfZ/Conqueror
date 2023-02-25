using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests;

public abstract class TestBase : IDisposable
{
    private readonly WebApplicationFactory<Program> applicationFactory;
    private readonly ServiceProvider clientServices;

    protected TestBase()
    {
        applicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // configure tests to produce logs; this is very useful to debug failing tests
            builder.ConfigureLogging(o => o.ClearProviders()
                                           .AddSimpleConsole()
                                           .SetMinimumLevel(LogLevel.Information));
        });

        HttpTestClient = applicationFactory.CreateClient(new() { AllowAutoRedirect = false });

        // create a dedicated service provider for resolving command and query clients
        // to prevent interference with other services from the actual application
        clientServices = new ServiceCollection().AddConquerorCQSHttpClientServices()
                                                .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    protected HttpClient HttpTestClient { get; }

    public void Dispose()
    {
        clientServices.Dispose();
        HttpTestClient.Dispose();
        applicationFactory.Dispose();
    }

    protected T ResolveOnServer<T>()
        where T : notnull => applicationFactory.Services.GetRequiredService<T>();

    protected T ResolveOnClient<T>()
        where T : notnull => clientServices.GetRequiredService<T>();
}
