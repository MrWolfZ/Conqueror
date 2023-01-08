using NUnit.Framework;

namespace Conqueror.Recipes.CQS.Basic.TestingHandlers.Tests;

[TestFixture]
public sealed class GetCounterValueQueryTests : TestBase
{
    private const string TestCounterName = "test-counter";

    private IGetCounterValueQueryHandler Handler => Resolve<IGetCounterValueQueryHandler>();

    private CountersRepository CountersRepository => Resolve<CountersRepository>();

    [Test]
    public void GivenNonExistingCounter_WhenGettingCounterValue_CounterNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<CounterNotFoundException>(() => Handler.ExecuteQuery(new(TestCounterName)));
    }

    [Test]
    public async Task GivenExistingCounter_WhenGettingCounterValue_CounterValueIsReturned()
    {
        await CountersRepository.SetCounterValue(TestCounterName, 10);

        var response = await Handler.ExecuteQuery(new(TestCounterName));

        Assert.That(response.CounterValue, Is.EqualTo(10));
    }
}
