using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Tests;

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
                                                .FinalizeConquerorRegistrations()
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

    protected T ResolveCommandClient<T>()
        where T : class, ICommandHandler => clientServices.GetRequiredService<ICommandClientFactory>().CreateCommandClient<T>(b => b.UseHttp(HttpTestClient));

    protected T ResolveQueryClient<T>()
        where T : class, IQueryHandler => clientServices.GetRequiredService<IQueryClientFactory>().CreateQueryClient<T>(b => b.UseHttp(HttpTestClient));
}
