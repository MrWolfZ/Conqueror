namespace Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests;

[TestFixture]
public sealed class IncrementCounterCommandTests : TestBase
{
    private IIncrementCounterCommandHandler CommandClient =>
        ResolveOnClient<ICommandClientFactory>().CreateCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(HttpTestClient));

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounter_CounterIsIncrementedAndNewValueIsReturned()
    {
        const string testCounterName = "testCounter";
        const int initialCounterValue = 10;
        const int expectedCounterValue = 11;

        var countersRepository = ResolveOnServer<CountersRepository>();

        await countersRepository.SetCounterValue(testCounterName, initialCounterValue);

        var response = await CommandClient.ExecuteCommand(new(testCounterName));

        var storedCounterValue = await countersRepository.GetCounterValue(testCounterName);

        Assert.That(response.NewCounterValue, Is.EqualTo(expectedCounterValue).And.EqualTo(storedCounterValue));
    }

    [Test]
    public void WhenExecutingInvalidCommand_ExecutionFailsWithValidationError()
    {
        var exception = Assert.ThrowsAsync<HttpCommandFailedException>(() => CommandClient.ExecuteCommand(new(string.Empty)));

        Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
