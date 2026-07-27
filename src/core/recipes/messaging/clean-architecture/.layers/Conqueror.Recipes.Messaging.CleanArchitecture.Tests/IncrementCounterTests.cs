namespace Conqueror.Recipes.Messaging.CleanArchitecture.Tests;

[TestFixture]
public class IncrementCounterTests
{
    private const string TestCounterName = "testCounter";
    private const string TestUserId = "user1";

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounter_ThenCounterIsIncrementedAndNewValueIsReturned()
    {
        await using var host = TestHost.Create();

        const int initialCounterValue = 10;
        const int expectedCounterValue = 11;

        var countersReadRepository = host.ResolveOnServer<ICountersReadRepository>();
        var countersWriteRepository = host.ResolveOnServer<ICountersWriteRepository>();

        await countersWriteRepository.SetCounterValue(TestCounterName, initialCounterValue);

        var response = await host.MessageSenders.For(IncrementCounter.T).Handle(new(TestCounterName, TestUserId));

        var storedCounterValue = await countersReadRepository.GetCounterValue(TestCounterName);

        Assert.That(response.NewCounterValue, Is.EqualTo(expectedCounterValue).And.EqualTo(storedCounterValue));
    }

    [Test]
    public async Task GivenNonExistingCounter_WhenIncrementingCounter_ThenCounterIsIncrementedAndNewValueIsReturned()
    {
        await using var host = TestHost.Create();

        const int expectedCounterValue = 1;

        var countersReadRepository = host.ResolveOnServer<ICountersReadRepository>();

        var response = await host.MessageSenders.For(IncrementCounter.T).Handle(new(TestCounterName, TestUserId));

        var storedCounterValue = await countersReadRepository.GetCounterValue(TestCounterName);

        Assert.That(response.NewCounterValue, Is.EqualTo(expectedCounterValue).And.EqualTo(storedCounterValue));
    }

    [Test]
    public async Task WhenIncrementingCounter_ThenMostRecentlyIncrementedCounterIsSetAndCanBeFetched()
    {
        await using var host = TestHost.Create();

        const string counterName1 = "counter1";
        const string counterName2 = "counter2";

        _ = await host.MessageSenders.For(IncrementCounter.T).Handle(new(counterName1, TestUserId));

        var response1 = await host.MessageSenders.For(GetMostRecentlyIncrementedCounterForUser.T).Handle(new(TestUserId));

        _ = await host.MessageSenders.For(IncrementCounter.T).Handle(new(counterName2, TestUserId));

        var response2 = await host.MessageSenders.For(GetMostRecentlyIncrementedCounterForUser.T).Handle(new(TestUserId));

        Assert.That(response1.CounterName, Is.EqualTo(counterName1));
        Assert.That(response2.CounterName, Is.EqualTo(counterName2));
    }
}
