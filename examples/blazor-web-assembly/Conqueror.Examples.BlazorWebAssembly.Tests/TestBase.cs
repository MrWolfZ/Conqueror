using System.Text.Json;
using Conqueror.CQS.Transport.Http.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Conqueror.Examples.BlazorWebAssembly.Tests;

public abstract class TestBase
{
    private WebApplicationFactory<Program>? applicationFactory;
    private HttpClient? client;

    protected HttpClient HttpClient
    {
        get
        {
            if (client == null)
            {
                throw new InvalidOperationException("test fixture must be initialized before using http client");
            }

            return client;
        }
    }

    private WebApplicationFactory<Program> ApplicationFactory
    {
        get
        {
            if (applicationFactory == null)
            {
                throw new InvalidOperationException("test fixture must be initialized before using application factory");
            }

            return applicationFactory;
        }
    }

    protected JsonSerializerOptions JsonSerializerOptions => Resolve<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

    protected THandler CreateCommandHttpClient<THandler>()
        where THandler : class, ICommandHandler => ResolveOnClient<ICommandClientFactory>().CreateCommandClient<THandler>(b => b.UseHttp(HttpClient));

    protected THandler CreateQueryHttpClient<THandler>()
        where THandler : class, IQueryHandler => ResolveOnClient<IQueryClientFactory>().CreateQueryClient<THandler>(b => b.UseHttp(HttpClient));

    [SetUp]
    public void SetUp()
    {
        applicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(ConfigureServerServices));

        client = applicationFactory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        applicationFactory?.Dispose();
        client?.Dispose();
    }

    protected virtual void ConfigureServerServices(IServiceCollection services)
    {
        services.AddSingleton<TestEventHub>();
        services.Replace(ServiceDescriptor.Transient<IEventHub>(p => p.GetRequiredService<TestEventHub>()));
    }

    protected virtual void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorCQSHttpClientServices(o =>
                    {
                        o.HttpClientFactory = _ => HttpClient;
                        o.JsonSerializerOptions = JsonSerializerOptions;
                    });
    }

    protected T Resolve<T>()
        where T : notnull => ApplicationFactory.Services.GetRequiredService<T>();

    protected T ResolveOnClient<T>()
        where T : notnull
    {
        var services = new ServiceCollection();
        ConfigureClientServices(services);
        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();
        return provider.GetRequiredService<T>();
    }
}
