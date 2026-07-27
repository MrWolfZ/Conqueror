namespace Conqueror.Recipes.Signalling.TestingHandlers.Tests;

using Microsoft.Extensions.DependencyInjection;

[TestFixture]
public class StatisticsHandlerTests
{
    private const string TestCounterName = "test-counter";

    [Test]
    public async Task GivenNoRecordedIncrements_WhenCounterIncrementedIsPublished_IncrementIsRecorded()
    {
        await using var serviceProvider = BuildServiceProvider();

        var publisher = serviceProvider
            .GetRequiredService<ISignalPublishers>()
            .For(CounterIncremented.T);

        await publisher.Handle(new(TestCounterName, 1));

        var statistics = serviceProvider.GetRequiredService<CounterStatistics>();

        Assert.That(statistics.RecordedIncrements, Is.EqualTo(1));
    }

    [Test]
    public async Task GivenRecordedIncrements_WhenCounterIncrementedIsPublished_IncrementIsRecorded()
    {
        await using var serviceProvider = BuildServiceProvider();

        var publisher = serviceProvider
            .GetRequiredService<ISignalPublishers>()
            .For(CounterIncremented.T);
        var statistics = serviceProvider.GetRequiredService<CounterStatistics>();

        statistics.RecordedIncrements = 10;

        await publisher.Handle(new(TestCounterName, 1));

        Assert.That(statistics.RecordedIncrements, Is.EqualTo(11));
    }

    private static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection()
            .AddSignalHandler<StatisticsHandler>()
            .AddSingleton<CounterStatistics>()
            .BuildServiceProvider();
    }
}
