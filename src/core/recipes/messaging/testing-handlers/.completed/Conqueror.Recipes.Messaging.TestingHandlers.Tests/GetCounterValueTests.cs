namespace Conqueror.Recipes.Messaging.TestingHandlers.Tests;

using Microsoft.Extensions.DependencyInjection;

[TestFixture]
public class GetCounterValueTests
{
    private const string TestCounterName = "test-counter";

    [Test]
    public async Task GivenNonExistingCounter_WhenGettingCounterValue_CounterNotFoundExceptionIsThrown()
    {
        await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(GetCounterValue.T);

        Assert.ThrowsAsync<CounterNotFoundException>(() => handler.Handle(new(TestCounterName)));
    }

    [Test]
    public async Task GivenExistingCounter_WhenGettingCounterValue_CounterValueIsReturned()
    {
        await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(GetCounterValue.T);
        var repository = serviceProvider.GetRequiredService<CountersRepository>();

        await repository.SetCounterValue(TestCounterName, 10);

        var response = await handler.Handle(new(TestCounterName));

        Assert.That(response.CounterValue, Is.EqualTo(10));
    }

    private static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection()
            .AddMessageHandler<GetCounterValueHandler>()
            .AddSingleton<CountersRepository>()
            .BuildServiceProvider();
    }
}
