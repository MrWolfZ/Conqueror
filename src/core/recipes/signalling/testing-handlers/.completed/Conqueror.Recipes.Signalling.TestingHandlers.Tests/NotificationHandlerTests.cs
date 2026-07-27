namespace Conqueror.Recipes.Signalling.TestingHandlers.Tests;

[TestFixture]
public class NotificationHandlerTests
{
    private const string TestCounterName = "test-counter";

    [Test]
    public async Task GivenCounterIncrementedBeyondThreshold_WhenSignalIsPublished_AdminNotificationIsSent()
    {
        await using var host = TestHost.Create();

        var publisher = host.SignalPublishers.For(CounterIncremented.T);

        await publisher.Handle(new(TestCounterName, 1000));

        await host.AdminNotificationServiceMock.Received(1)
            .SendCounterIncrementedBeyondThresholdNotification(TestCounterName);
    }

    [Test]
    public async Task GivenCounterIncrementedBelowThreshold_WhenSignalIsPublished_NoAdminNotificationIsSent()
    {
        await using var host = TestHost.Create();

        var publisher = host.SignalPublishers.For(CounterIncremented.T);

        await publisher.Handle(new(TestCounterName, 10));

        await host.AdminNotificationServiceMock.DidNotReceive()
            .SendCounterIncrementedBeyondThresholdNotification(TestCounterName);
    }
}
