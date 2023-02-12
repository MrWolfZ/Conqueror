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
        const string testCounterName = "testCounter";
        const int testCounterValue = 10;

        await applicationFactory.Services.GetRequiredService<CountersRepository>().SetCounterValue(testCounterName, testCounterValue);

        var response = await httpTestClient.GetFromJsonAsync<GetCounterValueQueryResponse>($"/api/v1/queries/getCounterValue?counterName={testCounterName}");

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.CounterExists, Is.True);
        Assert.That(response.CounterValue, Is.EqualTo(testCounterValue));
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
