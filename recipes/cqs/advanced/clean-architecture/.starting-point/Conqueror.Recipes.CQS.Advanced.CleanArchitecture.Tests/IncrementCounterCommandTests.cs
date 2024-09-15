namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Tests;

[TestFixture]
public sealed class IncrementCounterCommandTests : TestBase
{
    private const string TestCounterName = "testCounter";
    private const string TestUserId = "user1";

    private IIncrementCounterCommandHandler CommandClient => CreateCommandClient<IIncrementCounterCommandHandler>();

    private IGetMostRecentlyIncrementedCounterForUserQueryHandler GetMostRecentlyIncrementedCounterForUserQueryClient =>
        CreateQueryClient<IGetMostRecentlyIncrementedCounterForUserQueryHandler>();

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounter_CounterIsIncrementedAndNewValueIsReturned()
    {
        const int initialCounterValue = 10;
        const int expectedCounterValue = 11;

        var countersRepository = ResolveOnServer<CountersRepository>();

        await countersRepository.SetCounterValue(TestCounterName, initialCounterValue);

        var response = await CommandClient.ExecuteCommand(new(TestCounterName, TestUserId));

        var storedCounterValue = await countersRepository.GetCounterValue(TestCounterName);

        Assert.That(response.NewCounterValue, Is.EqualTo(expectedCounterValue).And.EqualTo(storedCounterValue));
    }

    [Test]
    public async Task GivenNonExistingCounter_WhenIncrementingCounter_CounterIsIncrementedAndNewValueIsReturned()
    {
        const int expectedCounterValue = 1;

        var countersRepository = ResolveOnServer<CountersRepository>();

        var response = await CommandClient.ExecuteCommand(new(TestCounterName, TestUserId));

        var storedCounterValue = await countersRepository.GetCounterValue(TestCounterName);

        Assert.That(response.NewCounterValue, Is.EqualTo(expectedCounterValue).And.EqualTo(storedCounterValue));
    }

    [Test]
    public async Task WhenIncrementingCounter_MostRecentlyIncrementedCounterIsSetAndCanBeFetched()
    {
        const string counterName1 = "counter1";
        const string counterName2 = "counter2";

        _ = await CommandClient.ExecuteCommand(new(counterName1, TestUserId));

        var response1 = await GetMostRecentlyIncrementedCounterForUserQueryClient.ExecuteQuery(new(TestUserId));

        _ = await CommandClient.ExecuteCommand(new(counterName2, TestUserId));

        var response2 = await GetMostRecentlyIncrementedCounterForUserQueryClient.ExecuteQuery(new(TestUserId));

        Assert.That(response1.CounterName, Is.EqualTo(counterName1));
        Assert.That(response2.CounterName, Is.EqualTo(counterName2));
    }
}
