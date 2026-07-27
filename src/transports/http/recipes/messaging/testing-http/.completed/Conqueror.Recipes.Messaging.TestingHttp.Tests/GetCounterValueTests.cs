namespace Conqueror.Recipes.Messaging.TestingHttp.Tests;

[TestFixture]
public class GetCounterValueTests
{
    private const string TestCounterName = "test-counter";

    [Test]
    public async Task GivenExistingCounter_WhenGettingCounterValue_ThenCounterValueIsReturned()
    {
        await using var host = TestHost.Create();

        const int counterValue = 10;

        await host.ResolveOnServer<CountersRepository>().SetCounterValue(TestCounterName, counterValue);

        var response = await host.HttpTestClient.GetFromJsonAsync<GetCounterValueResponse>(
            $"/api/v1/getCounterValue?counterName={TestCounterName}"
        );

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.CounterExists, Is.True);
        Assert.That(response.CounterValue, Is.EqualTo(counterValue));
    }

    [Test]
    public async Task GivenNonExistingCounter_WhenGettingCounterValue_ThenResponseIndicatesCounterDoesNotExist()
    {
        await using var host = TestHost.Create();

        var response = await host.HttpTestClient.GetFromJsonAsync<GetCounterValueResponse>(
            $"/api/v1/getCounterValue?counterName={TestCounterName}"
        );

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.CounterExists, Is.False);
        Assert.That(response.CounterValue, Is.Null);
    }
}
