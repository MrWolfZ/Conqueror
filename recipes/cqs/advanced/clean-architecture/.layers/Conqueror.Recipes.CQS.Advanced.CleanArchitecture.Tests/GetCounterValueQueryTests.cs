using Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;

namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Tests;

[TestFixture]
public sealed class GetCounterValueQueryTests : TestBase
{
    private const string TestCounterName = "testCounter";

    private IGetCounterValueQueryHandler QueryClient => ResolveQueryClient<IGetCounterValueQueryHandler>();

    [Test]
    public async Task GivenExistingCounter_WhenGettingCounterValue_CounterValueIsReturned()
    {
        const int testCounterValue = 10;

        await ResolveOnServer<ICountersWriteRepository>().SetCounterValue(TestCounterName, testCounterValue);

        var response = await QueryClient.ExecuteQuery(new(TestCounterName));

        Assert.That(response.CounterExists, Is.True);
        Assert.That(response.CounterValue, Is.EqualTo(testCounterValue));
    }

    [Test]
    public async Task GivenNonExistingCounter_WhenGettingCounterValue_NullIsReturned()
    {
        var response = await QueryClient.ExecuteQuery(new(TestCounterName));

        Assert.That(response.CounterExists, Is.False);
        Assert.That(response.CounterValue, Is.Null);
    }
}
