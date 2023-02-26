# Conqueror recipe (CQS Advanced): testing code which calls HTTP commands and queries

This recipe shows how simple it is to test your code which calls HTTP commands and queries with **Conqueror.CQS**.

This is an advanced recipe which builds upon the concepts introduced in the [recipes about CQS basics](../../../../../..#cqs-basics) as well as the recipes for [exposing commands and queries via HTTP](../exposing-via-http#readme) and [calling HTTP commands and queries from another application](../calling-http#readme). If you have not yet read those recipes, we recommend you take a look at them before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/testing-calling-http) and open the solution file in your IDE (note that you need to have [.NET 6 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/testing-calling-http).

The application, which we will be testing, is the client application we built in the recipe for [calling HTTP commands and queries from another application](../calling-http#readme) (with some minor changes). It is a console app that calls a server application which is managing a set of named counters. In code, the API of the server application is represented with the following types:

```cs
[HttpCommand(Version = "v1")]
public sealed record IncrementCounterCommand([Required] string CounterName);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

[HttpQuery(Version = "v1")]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);
```

> In this recipe we are testing a command line app, but the ideas also apply when testing server applications that call other server applications. The recipe for [moving from a modular monolith to a distributed system](../monolith-to-distributed#readme) explores how to test such distributed systems.

The console client application is used like this:

```txt
> cd Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Client
> dotnet run
input commands in format '<op> <counterName>' (e.g. 'inc test' or 'get test')
available operations: get, inc
> dotnet run inc test
incremented counter 'test'; new value: 1
> dotnet run inc
The CounterName field is required.
```

We are going to write tests, which execute the application in the same way a user would, i.e. we are going to invoke the code defined in [Program.cs](./Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Client/Program.cs). We will start by writing classic unit tests, which test the console application in isolation. Later, we will look at some alternative ways of testing our client application.

> If you look at the code for [Program.cs](./Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Client/Program.cs), you see that we are using a `HostBuilder` (via the [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) package) instead of a plain `ServiceCollection`. This allows us to configure services from within our tests by using a trick as shown below.

Let's start with unit testing the `get` operation. Create a new class `GetOperationTests.cs`:

```cs
namespace Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Client.Tests;

public sealed class GetOperationTests
{
}
```

To test the operation we need to execute our application with arguments like `get` and `inc`. We also need to be able to modify the app's services. One way to do this would be to refactor the application to provide an explicit entry point for doing this. However, as a showcase for how a console app can be tested without requiring such a refactoring, our test project contains a helper class [ProgramInvoker.cs](./Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Client.Tests/ProgramInvoker.cs). This class allows executing our application with given arguments and allows configuring the app's services.

Using this helper class, we can write a first test which asserts that a counter value can be fetched. For this test, we are using a unit testing approach, meaning we want to test the application in total isolation. Our application is using a client for the `IGetCounterValueQueryHandler` configured with the HTTP transport. Since we don't want to make a real HTTP call during the test, we need to replace the handler in the services. One way to do this is to use a capability of **Conqueror.CQS** which allows creating a handler from a delegate. With this, our test could look like this:

```cs
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
```

> Creating handlers from delegates like this is only recommended for testing purposes. In the application code itself, handlers shoud always be proper classes.

An alternative approach would be to use a mocking library like [Moq](https://github.com/moq/moq4) to create a mock for `IGetCounterValueQueryHandler` and add the mock to the services.

We can also write tests for error cases:

```cs
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
```

Next, we will test the `inc` operation. We are going to write tests in three different ways and will discuss the advantages and disadvantages of each. Create a new class `IncOperationTests.cs`:

```cs
namespace Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Client.Tests;

public sealed class IncOperationTests
{
}
```

The first test is a normal unit test like we wrote above:

```cs
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
```

The biggest advantage of this approach is that it is very simple. However, let's recall how the `IIncrementCounterCommandHandler` HTTP client is configured:

```cs
services.AddConquerorCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress, o => o.Headers.Add("my-header", "my-value")),
                                                                    pipeline => pipeline.UseDataAnnotationValidation())
```

The client adds a custom HTTP header and also declares a middleware pipeline that performs data annotation validation on the client before the command is sent to the server. With the unit testing approach above, these two aspects are not tested at all, since we completely replaced the `IIncrementCounterCommandHandler`. If we want to test these aspects, we need to do something else.

What we are going to do is to create a simple web host that is configured to use the ASP.NET Core test server. In this web server, we are going to add our command handler delegate and then we will configure the **Conqueror.CQS** HTTP client services to use the test server's test client. With that setup, the command client execution will go through the full middleware pipeline and HTTP invocation, allowing us to test those aspects as well.

> The test project is already configured with all the required dependencies. You need the [Microsoft.AspNetCore.Mvc.Testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) and [Conqueror.CQS.Transport.Http.Server.AspNetCore](https://www.nuget.org/packages/Conqueror.CQS.Transport.Http.Server.AspNetCore) packages.

Let's re-implement the test from above with this approach:

```cs
[Test]
public async Task GivenExistingCounter_WhenExecutingIncOperationViaHttp_PrintsIncrementedValue()
{
    const string counterName = "testCounter";
    const int incrementedCounterValue = 11;

    using var webHost = new WebHostBuilder()
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddControllers()
                                    .AddConquerorCQSHttpControllers();

                            services.AddConquerorCommandHandlerDelegate<IncrementCounterCommand, IncrementCounterCommandResponse>(async (command, _, _) =>
                            {
                                await Task.CompletedTask;
                                return command.CounterName == counterName ? new(incrementedCounterValue) : new(-1);
                            });
                        })
                        .Configure(app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapControllers()))
                        .Build();

    await webHost.StartAsync();

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
```

The biggest advantage of this approach is that it tests the full client configuration. The biggest downside is that it requires more setup code (which grows as the complexity of your command and query clients grows, e.g. with more complex pipelines and more HTTP-specifics like custom path conventions).

We want to write a few more tests that use the same setup. To prevent having to include the setup boilerplate code in each test, let's extract it into a helper method (it could also be extracted into a base class to allow re-using it across multiple test classes):

```cs
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
                         .UseEndpoints(endpoints => endpoints.MapControllers());
                  })
                  .Build();

    await webHost.StartAsync();

    return webHost;
}
```

With this helper method we can write tests to assert that the command validation works, to assert that the custom HTTP header is passed correctly, and to assert what happens when execution fails:

```cs
[Test]
public async Task WhenExecutingIncOperationWithInvalidCommand_PrintsErrorMessage()
{
    using var webHost = await StartServerApp();

    var testClient = webHost.GetTestClient();

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
    const int errorStatusCode = StatusCodes.Status500InternalServerError;

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
```

As you can see, the tests are very concise, even though they test the integration of many aspects.

The final approach we are going to look at is to use the real server application in our tests. This is only possible if the server and client live in the same code base, but provides the maximum amount of integration. Our test project already references the server project, so we can write a test as follows:

```cs
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
```

And that concludes this recipe for testing your code which calls HTTP commands and queries with **Conqueror.CQS**. Which of the test approaches we explored in this recipe you want to use is up to your and the requirements of the application you are building. In general, our recommendation is to **strive for the maximum amount of integration in your tests**. This means that, if possible, you should test against the real server application and use a mocked web application only for edge cases that cannot be reproduced using the server app. If your server and client applications are not part of the same code base, then use a mocked web application to test the full middleware pipeline and HTTP behavior. If you value simplicity over everything else, then overriding the client handler interface with a mocked handler is the way to go.

As the next step we recommend that you explore how to [create a clean architecture with commands and queries](../clean-architecture#readme). After you have finished that recipe you can take a look at the recipe for [moving from a modular monolith to a distributed system](../monolith-to-distributed#readme), which builds upon the concepts we just explored.

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.cqs.advanced.testing-calling-http]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
