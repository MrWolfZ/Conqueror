using Conqueror.Examples.CQS.GettingStarted.CustomHandlerInterface;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.CQS.GettingStarted.Tests;

public sealed class CustomInterfaceExampleQueryTests
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

    private ICustomInterfaceExampleQueryHandler QueryHandler =>
        ApplicationFactory.Services.GetRequiredService<ICustomInterfaceExampleQueryHandler>();

    [SetUp]
    public void SetUp()
    {
        applicationFactory = new();
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
