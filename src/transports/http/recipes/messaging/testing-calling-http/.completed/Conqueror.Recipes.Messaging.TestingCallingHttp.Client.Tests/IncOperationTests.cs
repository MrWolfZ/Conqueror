namespace Conqueror.Recipes.Messaging.TestingCallingHttp.Client.Tests;

[TestFixture]
public class IncOperationTests
{
    [Test]
    public async Task GivenExistingCounter_WhenExecutingIncOperation_PrintsIncrementedValue()
    {
        const string counterName = "testCounter";
        const int incrementedCounterValue = 11;

        var handler = Substitute.For<IncrementCounter.IHandler>();
        handler.Handle(Arg.Any<IncrementCounter>(), Arg.Any<CancellationToken>())
               .Returns(call => call.Arg<IncrementCounter>().CounterName == counterName
                                    ? new IncrementCounterResponse(incrementedCounterValue)
                                    : new IncrementCounterResponse(-1));

        var output = await ProgramInvoker.Invoke(services => services.AddSingleton(handler), "inc", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"incremented counter '{counterName}'; new value: {incrementedCounterValue}"));
    }

    [Test]
    public async Task GivenExistingCounter_WhenExecutingIncOperationViaHttp_PrintsIncrementedValue()
    {
        const string counterName = "testCounter";
        const int incrementedCounterValue = 11;

        using var webHost = await StartServerApp(services =>
            services.AddHttpMessageHandlerDelegate(IncrementCounter.T, async (message, _, _) =>
            {
                await Task.CompletedTask;
                return message.CounterName == counterName ? new(incrementedCounterValue) : new IncrementCounterResponse(-1);
            }));

        var testClient = webHost.GetTestClient();

        var output = await ProgramInvoker.Invoke(services => services.AddSingleton(testClient), "inc", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"incremented counter '{counterName}'; new value: {incrementedCounterValue}"));
    }

    [Test]
    public async Task WhenExecutingIncOperationWithInvalidMessage_PrintsErrorMessage()
    {
        var requestWasReceived = false;

        using var webHost = await StartServerApp(configureApp: app => app.Use((ctx, next) =>
        {
            requestWasReceived = true;
            return next();
        }));

        var testClient = webHost.GetTestClient();

        // call inc operation without a counter name
        var output = await ProgramInvoker.Invoke(services => services.AddSingleton(testClient), "inc");

        Assert.That(output.Trim(), Is.EqualTo("counter name must not be empty"));
        Assert.That(requestWasReceived, Is.False);
    }

    [Test]
    public async Task WhenExecutingIncOperation_CustomHttpHeaderIsPassed()
    {
        const string counterName = "testCounter";
        const string headerName = "my-header";
        const string expectedHeaderValue = "my-value";

        string? seenHeaderValue = null;

        using var webHost = await StartServerApp(configureApp: app => app.Use((ctx, next) =>
        {
            seenHeaderValue = ctx.Request.Headers.TryGetValue(headerName, out var v) ? v.ToString() : null;
            return next();
        }));

        var testClient = webHost.GetTestClient();

        await ProgramInvoker.Invoke(services => services.AddSingleton(testClient), "inc", counterName);

        Assert.That(seenHeaderValue, Is.EqualTo(expectedHeaderValue));
    }

    [Test]
    public async Task GivenServerThatFailsRequests_WhenExecutingIncOperation_ErrorMessageIsPrinted()
    {
        const string counterName = "testCounter";
        const int errorStatusCode = StatusCodes.Status502BadGateway;

        using var webHost = await StartServerApp(configureApp: app => app.Use((HttpContext ctx, Func<Task> _) =>
        {
            ctx.Response.StatusCode = errorStatusCode;
            return Task.CompletedTask;
        }));

        var testClient = webHost.GetTestClient();

        var output = await ProgramInvoker.Invoke(services => services.AddSingleton(testClient), "inc", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"HTTP message failed with status code {errorStatusCode}"));
    }

    [Test]
    public async Task GivenExistingCounter_WhenExecutingIncOperationWithRealServerApp_PrintsIncrementedValue()
    {
        const string counterName = "testCounter";

        // the server app could additionally be configured with mocks as necessary
        await using var serverApp = new WebApplicationFactory<ServerProgram>();

        var testClient = serverApp.CreateClient();

        var output1 = await ProgramInvoker.Invoke(services => services.AddSingleton(testClient), "inc", counterName);

        var output2 = await ProgramInvoker.Invoke(services => services.AddSingleton(testClient), "inc", counterName);

        Assert.That(output1.Trim(), Is.EqualTo($"incremented counter '{counterName}'; new value: {1}"));
        Assert.That(output2.Trim(), Is.EqualTo($"incremented counter '{counterName}'; new value: {2}"));
    }

    private static async Task<IWebHost> StartServerApp(Action<IServiceCollection>? configureServices = null,
                                                       Action<IApplicationBuilder>? configureApp = null)
    {
        var webHost = new WebHostBuilder()
                      .UseTestServer()
                      .ConfigureServices(services =>
                      {
                          services.AddRouting()
                                  .AddConquerorHttpServerAspNetCore();

                          configureServices?.Invoke(services);
                      })
                      .Configure(app =>
                      {
                          configureApp?.Invoke(app);

                          app.UseRouting()
                             .UseEndpoints(endpoints => endpoints.MapMessageEndpoints());
                      })
                      .Build();

        await webHost.StartAsync();

        return webHost;
    }
}
