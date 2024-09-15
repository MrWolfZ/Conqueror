using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Application;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Contracts;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Contracts;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Tests;

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

        var countersReadRepository = ResolveOnServer<ICountersReadRepository>();
        var countersWriteRepository = ResolveOnServer<ICountersWriteRepository>();

        await countersWriteRepository.SetCounterValue(TestCounterName, initialCounterValue);

        var response = await CommandClient.Handle(new(TestCounterName, TestUserId));

        var storedCounterValue = await countersReadRepository.GetCounterValue(TestCounterName);

        Assert.That(response.NewCounterValue, Is.EqualTo(expectedCounterValue).And.EqualTo(storedCounterValue));
    }

    [Test]
    public async Task GivenNonExistingCounter_WhenIncrementingCounter_CounterIsIncrementedAndNewValueIsReturned()
    {
        const int expectedCounterValue = 1;

        var countersRepository = ResolveOnServer<ICountersReadRepository>();

        var response = await CommandClient.Handle(new(TestCounterName, TestUserId));

        var storedCounterValue = await countersRepository.GetCounterValue(TestCounterName);

        Assert.That(response.NewCounterValue, Is.EqualTo(expectedCounterValue).And.EqualTo(storedCounterValue));
    }

    [Test]
    public async Task WhenIncrementingCounter_MostRecentlyIncrementedCounterIsSetAndCanBeFetched()
    {
        const string counterName1 = "counter1";
        const string counterName2 = "counter2";

        _ = await CommandClient.Handle(new(counterName1, TestUserId));

        var response1 = await GetMostRecentlyIncrementedCounterForUserQueryClient.Handle(new(TestUserId));

        _ = await CommandClient.Handle(new(counterName2, TestUserId));

        var response2 = await GetMostRecentlyIncrementedCounterForUserQueryClient.Handle(new(TestUserId));

        Assert.That(response1.CounterName, Is.EqualTo(counterName1));
        Assert.That(response2.CounterName, Is.EqualTo(counterName2));
    }
}
