namespace Conqueror.Recipes.Messaging.TestingHandlers.Tests;

[TestFixture]
public class IncrementCounterTests : TestBase
{
    private const string TestCounterName = "test-counter";

    private IncrementCounter.IHandler Handler => MessageSenders.For(IncrementCounter.T);

    private CountersRepository CountersRepository => Resolve<CountersRepository>();

    [Test]
    public async Task GivenNonExistingCounter_WhenIncrementingCounter_CounterIsCreatedAndInitialValueIsReturned()
    {
        var response = await Handler.Handle(new(TestCounterName));

        var storedCounterValue = await CountersRepository.GetCounterValue(TestCounterName);

        Assert.That(storedCounterValue, Is.EqualTo(1).And.EqualTo(response.NewCounterValue));
    }

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounter_CounterIsIncrementedAndValueIsReturned()
    {
        await CountersRepository.SetCounterValue(TestCounterName, 10);

        var response = await Handler.Handle(new(TestCounterName));

        var storedCounterValue = await CountersRepository.GetCounterValue(TestCounterName);

        Assert.That(storedCounterValue, Is.EqualTo(11).And.EqualTo(response.NewCounterValue));
    }

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounterAboveThreshold_AdminNotificationIsSent()
    {
        await CountersRepository.SetCounterValue(TestCounterName, 999);

        _ = await Handler.Handle(new(TestCounterName));

        await AdminNotificationServiceMock.Received(1)
            .SendCounterIncrementedBeyondThresholdNotification(TestCounterName);
    }

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounterBelowThreshold_NoAdminNotificationIsSent()
    {
        await CountersRepository.SetCounterValue(TestCounterName, 10);

        _ = await Handler.Handle(new(TestCounterName));

        await AdminNotificationServiceMock.DidNotReceive()
            .SendCounterIncrementedBeyondThresholdNotification(TestCounterName);
    }
}
