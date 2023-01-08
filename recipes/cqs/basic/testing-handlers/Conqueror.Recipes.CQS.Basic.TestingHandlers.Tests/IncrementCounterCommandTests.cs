using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.Recipes.CQS.Basic.TestingHandlers.Tests;

[TestFixture]
public sealed class IncrementCounterCommandTests
{
    private const string TestCounterName = "test-counter";

    [Test]
    public async Task GivenNonExistingCounter_WhenIncrementingCounter_CounterIsCreatedAndIncremented()
    {
        await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IIncrementCounterCommandHandler>();
        var repository = serviceProvider.GetRequiredService<CountersRepository>();

        _ = await handler.ExecuteCommand(new(TestCounterName));

        var storedCounterValue = await repository.GetCounterValue(TestCounterName);

        Assert.That(storedCounterValue, Is.EqualTo(1));
    }

    [Test]
    public async Task GivenNonExistingCounter_WhenIncrementingCounter_NewCounterValueIsReturned()
    {
        await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IIncrementCounterCommandHandler>();

        var response = await handler.ExecuteCommand(new(TestCounterName));

        Assert.That(response.NewCounterValue, Is.EqualTo(1));
    }

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounter_CounterIsIncremented()
    {
        await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IIncrementCounterCommandHandler>();
        var repository = serviceProvider.GetRequiredService<CountersRepository>();

        await repository.SetCounterValue(TestCounterName, 10);

        _ = await handler.ExecuteCommand(new(TestCounterName));

        var storedCounterValue = await repository.GetCounterValue(TestCounterName);

        Assert.That(storedCounterValue, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounter_NewCounterValueIsReturned()
    {
        await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IIncrementCounterCommandHandler>();
        var repository = serviceProvider.GetRequiredService<CountersRepository>();

        await repository.SetCounterValue(TestCounterName, 10);

        var response = await handler.ExecuteCommand(new(TestCounterName));

        Assert.That(response.NewCounterValue, Is.EqualTo(11));
    }

    private static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection().AddConquerorCQS()
                                      .AddTransient<IncrementCounterCommandHandler>()
                                      .AddSingleton<CountersRepository>()
                                      .FinalizeConquerorRegistrations()
                                      .BuildServiceProvider();
    }
}
