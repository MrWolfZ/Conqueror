namespace Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests;

[TestFixture]
public sealed class IncrementCounterCommandTests : TestBase
{
    private const string TestCounterName = "test-counter";

    private IIncrementCounterCommandHandler Handler => Resolve<IIncrementCounterCommandHandler>();

    private CountersRepository CountersRepository => Resolve<CountersRepository>();

    [Test]
    public async Task GivenNonExistingCounter_WhenIncrementingCounter_CounterIsCreatedAndInitialValueIsReturned()
    {
        var response = await Handler.Handle(new(TestCounterName));

        // we validate the result of the command by checking the repository directly, which is a small
        // violation of the black-box testing approach; the alternative would be to fetch the counter's
        // value with the `GetCounterValueQuery`, but this adds a dependency to this query to the tests
        // for the command; a third option would be to not test the command and query separately but
        // instead test the whole counter "domain" together by creating more end-to-end test cases like
        // `GivenNonExistingCounter_WhenIncrementingCounter_CounterIsCreatedAndValueCanBeFetchedByQuery`;
        // which of these approaches you choose is up to you to decide, they all have different trade-offs
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

        AdminNotificationServiceMock.Verify(s => s.SendCounterIncrementedBeyondThresholdNotification(TestCounterName));
    }

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounterBelowThreshold_NoAdminNotificationIsSent()
    {
        await CountersRepository.SetCounterValue(TestCounterName, 10);

        _ = await Handler.Handle(new(TestCounterName));

        AdminNotificationServiceMock.Verify(s => s.SendCounterIncrementedBeyondThresholdNotification(TestCounterName), Times.Never);
    }
}
