using Conqueror.Examples.CQS.GettingStarted.MiddlewaresExample;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.Examples.CQS.GettingStarted.Tests;

public sealed class MiddlewaresExampleQueryTests
{
    private WebApplicationFactory<Program>? applicationFactory;

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

    private IMiddlewaresExampleQueryHandler QueryHandler => ApplicationFactory.Services.GetRequiredService<IMiddlewaresExampleQueryHandler>();

    [SetUp]
    public void SetUp()
    {
        applicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace)));
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
