using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.Recipes.CQS.Basic.TestingHandlers.Tests;

[TestFixture]
public sealed class GetCounterValueQueryTests
{
    private const string TestCounterName = "test-counter";

    [Test]
    public async Task GivenNonExistingCounter_WhenGettingCounterValue_CounterNotFoundExceptionIsThrown()
    {
        await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IGetCounterValueQueryHandler>();

        Assert.ThrowsAsync<CounterNotFoundException>(() => handler.ExecuteQuery(new(TestCounterName)));
    }

    [Test]
    public async Task GivenExistingCounter_WhenGettingCounterValue_CounterValueIsReturned()
    {
        await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IGetCounterValueQueryHandler>();
        var repository = serviceProvider.GetRequiredService<CountersRepository>();

        await repository.SetCounterValue(TestCounterName, 10);

        var response = await handler.ExecuteQuery(new(TestCounterName));

        Assert.That(response.CounterValue, Is.EqualTo(10));
    }

    private static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection().AddConquerorCQS()
                                      .AddTransient<GetCounterValueQueryHandler>()
                                      .AddSingleton<CountersRepository>()
                                      .FinalizeConquerorRegistrations()
                                      .BuildServiceProvider();
    }
}
