namespace Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Client.Tests;

[TestFixture]
public sealed class IncOperationTests
{
    [Test]
    public async Task GivenExistingCounter_WhenExecutingIncOperation_PrintsIncrementedValue()
    {
        const string counterName = "testCounter";
        const int incrementedCounterValue = 11;

        var output = await ProgramInvoker.Invoke(services =>
        {
            services.AddConquerorCommandHandlerDelegate<IncrementCounterCommand, IncrementCounterCommandResponse>(async (command, _, _) =>
            {
                await Task.CompletedTask;
                return command.CounterName == counterName ? new(incrementedCounterValue) : new(-1);
            });
        }, "inc", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"incremented counter '{counterName}'; new value: {incrementedCounterValue}"));
    }

    [Test]
    public async Task GivenExistingCounter_WhenExecutingIncOperationViaHttp_PrintsIncrementedValue()
    {
        const string counterName = "testCounter";
        const int incrementedCounterValue = 11;

        using var webHost = await StartServerApp(services =>
        {
            services.AddConquerorCommandHandlerDelegate<IncrementCounterCommand, IncrementCounterCommandResponse>(async (command, _, _) =>
            {
                await Task.CompletedTask;
                return command.CounterName == counterName ? new(incrementedCounterValue) : new(-1);
            });
        });

        var testClient = webHost.GetTestClient();

        var output = await ProgramInvoker.Invoke(services =>
        {
            // configure the HTTP client options to use the test client for all command and query clients;
            // if necessary you could also set different clients for each command and query, e.g. like this:
            // o.UseHttpClientForCommand<IncrementCounterCommand>(testClient)
            services.ConfigureConquerorCQSHttpClientOptions(o => o.UseHttpClient(testClient));
        }, "inc", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"incremented counter '{counterName}'; new value: {incrementedCounterValue}"));
    }

    [Test]
    public async Task WhenExecutingIncOperationWithInvalidCommand_PrintsErrorMessage()
    {
        using var webHost = await StartServerApp();

        var testClient = webHost.GetTestClient();

        // call inc operation without a counter name
        var output = await ProgramInvoker.Invoke(
            services => services.ConfigureConquerorCQSHttpClientOptions(o => o.UseHttpClient(testClient)),
            "inc");

        Assert.That(output.Trim(), Is.EqualTo("The CounterName field is required."));
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

        await ProgramInvoker.Invoke(
            services => services.ConfigureConquerorCQSHttpClientOptions(o => o.UseHttpClient(testClient)),
            "inc", counterName);

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

        var output = await ProgramInvoker.Invoke(
            services => services.ConfigureConquerorCQSHttpClientOptions(o => o.UseHttpClient(testClient)),
            "inc", counterName);

        Assert.That(output.Trim(), Is.EqualTo($"HTTP command failed with status code {errorStatusCode}"));
    }

    [Test]
    public async Task GivenExistingCounter_WhenExecutingIncOperationWithRealServerApp_PrintsIncrementedValue()
    {
        const string counterName = "testCounter";

        // the server app could additionally be configured with mocks as necessary
        await using var serverApp = new WebApplicationFactory<ServerProgram>();

        var testClient = serverApp.CreateClient();

        var output1 = await ProgramInvoker.Invoke(
            services => services.ConfigureConquerorCQSHttpClientOptions(o => o.UseHttpClient(testClient)),
            "inc", counterName);

        var output2 = await ProgramInvoker.Invoke(
            services => services.ConfigureConquerorCQSHttpClientOptions(o => o.UseHttpClient(testClient)),
            "inc", counterName);

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
                          services.AddControllers()
                                  .AddConquerorCQSHttpControllers();

                          configureServices?.Invoke(services);
                      })
                      .Configure(app =>
                      {
                          configureApp?.Invoke(app);

                          app.UseRouting()
                             .UseConqueror()
                             .UseEndpoints(endpoints => endpoints.MapControllers());
                      })
                      .Build();

        await webHost.StartAsync();

        return webHost;
    }
}
