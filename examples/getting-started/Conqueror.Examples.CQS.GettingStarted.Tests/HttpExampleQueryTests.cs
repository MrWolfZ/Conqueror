using Conqueror.Examples.CQS.GettingStarted.HttpExample;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.CQS.GettingStarted.Tests;

public sealed class HttpExampleQueryTests
{
    private WebApplicationFactory<Program>? applicationFactory;
    private HttpClient? client;
    private IServiceProvider? clientServiceProvider;

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

    private IServiceProvider ClientServiceProvider
    {
        get
        {
            if (clientServiceProvider == null)
            {
                throw new InvalidOperationException("test fixture must be initialized before using client service provider");
            }

            return clientServiceProvider;
        }
    }

    private IHttpExampleQueryHandler QueryHandler => ClientServiceProvider.GetRequiredService<IQueryClientFactory>()
                                                                          .CreateQueryClient<IHttpExampleQueryHandler>(b => b.UseHttp(new("http://localhost")));

    [SetUp]
    public void SetUp()
    {
        applicationFactory = new();

        clientServiceProvider = new ServiceCollection().AddConquerorCQSHttpClientServices(o => o.UseHttpClient(HttpClient))
                                                       .BuildServiceProvider();

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
