using Conqueror.Examples.CQS.GettingStarted.Simple;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.CQS.GettingStarted.Tests;

public sealed class SimpleEchoQueryTests
{
    private readonly IServiceProvider serviceProvider;

    public SimpleEchoQueryTests()
    {
        var services = new ServiceCollection();

        services.AddConquerorQueryHandler<SimpleEchoQueryHandler>();

        serviceProvider = services.BuildServiceProvider();
    }

    private IQueryHandler<SimpleEchoQuery, SimpleEchoQueryResponse> QueryHandler =>
        serviceProvider.GetRequiredService<IQueryHandler<SimpleEchoQuery, SimpleEchoQueryResponse>>();

    [Test]
    public async Task GivenParameter_ReturnsParameterValue()
    {
        var response = await QueryHandler.Handle(new(10), CancellationToken.None);
        Assert.That(response.Value, Is.EqualTo(10));
    }
}
