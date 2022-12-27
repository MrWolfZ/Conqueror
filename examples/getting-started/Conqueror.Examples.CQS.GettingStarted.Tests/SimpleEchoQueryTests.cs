using Conqueror.Examples.CQS.GettingStarted.Simple;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.CQS.GettingStarted.Tests;

public sealed class SimpleEchoQueryTests
{
    private readonly IServiceProvider serviceProvider;

    public SimpleEchoQueryTests()
    {
        var services = new ServiceCollection();

        services.AddConquerorCQS()
                .AddTransient<SimpleEchoQueryHandler>();

        serviceProvider = services.FinalizeConquerorRegistrations().BuildServiceProvider();
    }

    private IQueryHandler<SimpleEchoQuery, SimpleEchoQueryResponse> QueryHandler => 
        serviceProvider.GetRequiredService<IQueryHandler<SimpleEchoQuery, SimpleEchoQueryResponse>>();

    [Test]
    public async Task GivenParameter_ReturnsParameterValue()
    {
        var response = await QueryHandler.ExecuteQuery(new(10), CancellationToken.None);
        Assert.That(response.Value, Is.EqualTo(10));
    }
}