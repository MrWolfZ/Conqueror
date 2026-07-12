namespace Conqueror.Recipes.Messaging.TestingHandlers.Tests;

[TestFixture]
public class IncrementCounterTests
{
    private const string TestCounterName = "test-counter";

    [Test]
    public async Task GivenNonExistingCounter_WhenIncrementingCounter_CounterIsCreatedAndInitialValueIsReturned()
    {
        await using var host = TestHost.Create();

        var handler = host.MessageSenders.For(IncrementCounter.T);

        var response = await handler.Handle(new(TestCounterName));

        var storedCounterValue = await host.Resolve<CountersRepository>().GetCounterValue(TestCounterName);

        Assert.That(storedCounterValue, Is.EqualTo(1).And.EqualTo(response.NewCounterValue));
    }

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounter_CounterIsIncrementedAndValueIsReturned()
    {
        await using var host = TestHost.Create();

        var handler = host.MessageSenders.For(IncrementCounter.T);

        await host.Resolve<CountersRepository>().SetCounterValue(TestCounterName, 10);

        var response = await handler.Handle(new(TestCounterName));

        var storedCounterValue = await host.Resolve<CountersRepository>().GetCounterValue(TestCounterName);

        Assert.That(storedCounterValue, Is.EqualTo(11).And.EqualTo(response.NewCounterValue));
    }

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounterAboveThreshold_AdminNotificationIsSent()
    {
        await using var host = TestHost.Create();

        var handler = host.MessageSenders.For(IncrementCounter.T);

        await host.Resolve<CountersRepository>().SetCounterValue(TestCounterName, 999);

        _ = await handler.Handle(new(TestCounterName));

        await host.AdminNotificationServiceMock.Received(1)
            .SendCounterIncrementedBeyondThresholdNotification(TestCounterName);
    }

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounterBelowThreshold_NoAdminNotificationIsSent()
    {
        await using var host = TestHost.Create();

        var handler = host.MessageSenders.For(IncrementCounter.T);

        await host.Resolve<CountersRepository>().SetCounterValue(TestCounterName, 10);

        _ = await handler.Handle(new(TestCounterName));

        await host.AdminNotificationServiceMock.DidNotReceive()
            .SendCounterIncrementedBeyondThresholdNotification(TestCounterName);
    }
}
