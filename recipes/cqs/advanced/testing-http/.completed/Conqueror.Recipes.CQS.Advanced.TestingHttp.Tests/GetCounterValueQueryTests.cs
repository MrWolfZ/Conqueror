namespace Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests;

[TestFixture]
public sealed class GetCounterValueQueryTests : IDisposable
{
    private readonly WebApplicationFactory<Program> applicationFactory = new();
    private readonly HttpClient httpTestClient;

    public GetCounterValueQueryTests()
    {
        httpTestClient = applicationFactory.CreateClient();
    }

    public void Dispose()
    {
        httpTestClient.Dispose();
        applicationFactory.Dispose();
    }

    [Test]
    public async Task GivenExistingCounter_WhenGettingCounterValue_CounterValueIsReturned()
    {
        const string counterName = "testCounter";
        const int counterValue = 10;

        await applicationFactory.Services.GetRequiredService<CountersRepository>().SetCounterValue(counterName, counterValue);

        var response = await httpTestClient.GetFromJsonAsync<GetCounterValueQueryResponse>($"/api/v1/queries/getCounterValue?counterName={counterName}");

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.CounterExists, Is.True);
        Assert.That(response.CounterValue, Is.EqualTo(counterValue));
    }

    [Test]
    public async Task WhenExecutingInvalidQuery_RequestFailsWithBadRequest()
    {
        // omit counterName parameter to make query invalid
        var response = await httpTestClient.GetAsync("/api/v1/queries/getCounterValue");

        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
