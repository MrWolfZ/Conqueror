namespace Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Client.Tests;

[TestFixture]
public sealed class GetOperationTests
{
    [Test]
    public async Task GivenExistingCounter_WhenExecutingGetOperation_PrintsCounterValue()
    {
        const string counterName = "testCounter";
        const int counterValue = 10;

        var output = await ProgramInvoker.Invoke(services =>
        {
            // this adds a handler of type IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>, which
            // in turn is used when the client of type `IGetCounterValueQueryHandler` is executed
            services.AddConquerorQueryHandlerDelegate<GetCounterValueQuery, GetCounterValueQueryResponse>(async (query, _, _) =>
            {
                await Task.CompletedTask;
                return query.CounterName == counterName ? new(true, counterValue) : new GetCounterValueQueryResponse(false, null);
            });
        }, "get", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"counter '{counterName}' value: {counterValue}"));
    }

    [Test]
    public async Task WhenExecutingGetOperationFailsWithHttpError_PrintsErrorMessage()
    {
        const string counterName = "testCounter";
        const HttpStatusCode errorStatusCode = HttpStatusCode.InternalServerError;

        var output = await ProgramInvoker.Invoke(services =>
        {
            services.AddConquerorQueryHandlerDelegate<GetCounterValueQuery, GetCounterValueQueryResponse>(async (query, _, _) =>
            {
                await Task.CompletedTask;
                throw new HttpQueryFailedException("query failed", new() { StatusCode = errorStatusCode });
            });
        }, "get", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"HTTP query failed with status code {(int)errorStatusCode}"));
    }
}
