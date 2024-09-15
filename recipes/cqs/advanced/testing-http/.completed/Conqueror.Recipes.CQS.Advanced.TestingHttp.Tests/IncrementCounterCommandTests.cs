namespace Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests;

[TestFixture]
public sealed class IncrementCounterCommandTests : TestBase
{
    private IIncrementCounterCommandHandler CommandClient =>
        ResolveOnClient<ICommandClientFactory>().CreateCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(new("http://localhost")));

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounter_CounterIsIncrementedAndNewValueIsReturned()
    {
        const string counterName = "testCounter";
        const int initialCounterValue = 10;
        const int expectedCounterValue = 11;

        var countersRepository = ResolveOnServer<CountersRepository>();

        await countersRepository.SetCounterValue(counterName, initialCounterValue);

        var response = await CommandClient.Handle(new(counterName));

        var storedCounterValue = await countersRepository.GetCounterValue(counterName);

        Assert.That(response.NewCounterValue, Is.EqualTo(expectedCounterValue).And.EqualTo(storedCounterValue));
    }

    [Test]
    public void WhenExecutingInvalidCommand_ExecutionFailsWithValidationError()
    {
        var exception = Assert.ThrowsAsync<HttpCommandFailedException>(() => CommandClient.Handle(new(string.Empty)));

        Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
