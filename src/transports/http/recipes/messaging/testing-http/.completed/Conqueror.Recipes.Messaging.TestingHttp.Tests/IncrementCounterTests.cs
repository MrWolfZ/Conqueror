namespace Conqueror.Recipes.Messaging.TestingHttp.Tests;

[TestFixture]
public class IncrementCounterTests
{
    private const string TestCounterName = "test-counter";

    [Test]
    public async Task GivenExistingCounter_WhenIncrementingCounter_ThenCounterIsIncrementedAndNewValueIsReturned()
    {
        await using var host = TestHost.Create();

        await host.ResolveOnServer<CountersRepository>().SetCounterValue(TestCounterName, 10);

        var response = await host
            .MessageSenders.For(IncrementCounter.T)
            .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(host.HttpTestClient))
            .Handle(new(TestCounterName));

        var storedCounterValue = await host.ResolveOnServer<CountersRepository>().GetCounterValue(TestCounterName);

        Assert.That(response.NewCounterValue, Is.EqualTo(11).And.EqualTo(storedCounterValue));
    }

    [Test]
    public async Task GivenCounterAtValueLimit_WhenIncrementingCounter_ThenMessageFailsWithInternalServerError()
    {
        await using var host = TestHost.Create();

        await host.ResolveOnServer<CountersRepository>().SetCounterValue(TestCounterName, 1000);

        var exception = Assert.ThrowsAsync<HttpMessageFailedOnClientException>(() =>
            host.MessageSenders.For(IncrementCounter.T)
                .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(host.HttpTestClient))
                .Handle(new(TestCounterName))
        );

        Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }
}
