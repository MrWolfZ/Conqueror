using Conqueror.CQS.Transport.Http.Client;
using Conqueror.Examples.CQS.GettingStarted.HttpExample;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.CQS.GettingStarted.Tests;

public sealed class HttpExampleQueryTests
{
    private WebApplicationFactory<Program>? applicationFactory;
    private HttpClient? client;

    private HttpClient HttpClient
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

    private IHttpExampleQueryHandler QueryHandler => ApplicationFactory.Services
                                                                       .GetRequiredService<IQueryClientFactory>()
                                                                       .CreateQueryClient<IHttpExampleQueryHandler>(b => b.UseHttp(HttpClient));

    [SetUp]
    public void SetUp()
    {
        applicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services => services.AddConquerorCQSHttpClientServices()));

        client = applicationFactory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        applicationFactory?.Dispose();
    }

    [Test]
    public async Task GivenParameter_ReturnsParameterValue()
    {
        var response = await QueryHandler.ExecuteQuery(new(10), CancellationToken.None);
        Assert.That(response.Value, Is.EqualTo(10));
    }
}