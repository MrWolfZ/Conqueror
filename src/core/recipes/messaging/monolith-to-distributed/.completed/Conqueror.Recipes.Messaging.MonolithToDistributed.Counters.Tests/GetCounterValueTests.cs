namespace Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Tests;

[TestFixture]
public class GetCounterValueTests
{
    private const string TestCounterName = "testCounter";

    [Test]
    public async Task GivenExistingCounter_WhenGettingCounterValue_ThenCounterValueIsReturned()
    {
        await using var host = TestHost.Create();

        const int counterValue = 10;

        await host.ResolveOnServer<ICountersWriteRepository>().SetCounterValue(TestCounterName, counterValue);

        var response = await host.MessageSenders.For(GetCounterValue.T).Handle(new(TestCounterName));

        Assert.That(response.CounterExists, Is.True);
        Assert.That(response.CounterValue, Is.EqualTo(counterValue));
    }

    [Test]
    public async Task GivenNonExistingCounter_WhenGettingCounterValue_ThenNullIsReturned()
    {
        await using var host = TestHost.Create();

        var response = await host.MessageSenders.For(GetCounterValue.T).Handle(new(TestCounterName));

        Assert.That(response.CounterExists, Is.False);
        Assert.That(response.CounterValue, Is.Null);
    }
}
