# Conqueror recipe (Messaging): testing code which calls HTTP messages

This recipe shows how simple it is to test your code which calls HTTP messages with **Conqueror**.

This is an advanced recipe which builds upon the concepts introduced in the [recipes about messaging basics](../../../../../..#messaging-basics) as well as the recipes for [exposing messages via HTTP](../exposing-via-http#readme) and [calling HTTP messages from another application](../calling-http#readme). If you have not yet read those recipes, we recommend you take a look at them before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/transports/http/recipes/messaging/testing-calling-http) and open the solution file in your IDE (note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/transports/http/recipes/messaging/testing-calling-http).

The application, which we will be testing, is the client application we built in the recipe for [calling HTTP messages from another application](../calling-http#readme) (with some minor changes). It is a console app that calls a server application which is managing a set of named counters. In code, the API of the server application is represented with the following types:

```cs
[HttpMessage<IncrementCounterResponse>(Version = "v1")]
public partial record IncrementCounter(string CounterName);

public record IncrementCounterResponse(int NewCounterValue);

[HttpMessage<GetCounterValueResponse>(HttpMethod = "GET", Version = "v1")]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);
```

> In this recipe we are testing a command line app, but the ideas also apply when testing server applications that call other server applications. The recipe for [moving from a modular monolith to a distributed system](../../../../../core/recipes/messaging/monolith-to-distributed#readme) explores how to test such distributed systems.

The console client application is used like this:

```txt
> cd Conqueror.Recipes.Messaging.TestingCallingHttp.Client
> dotnet run
input commands in format '<op> <counterName>' (e.g. 'inc test' or 'get test')
available operations: inc, get
> dotnet run inc test
incremented counter 'test'; new value: 1
> dotnet run inc
counter name must not be empty
```

We are going to write tests, which execute the application in the same way a user would, i.e. we are going to invoke the code defined in [Program.cs](Conqueror.Recipes.Messaging.TestingCallingHttp.Client/Program.cs). We will start by writing classic unit tests, which test the console application in isolation. Later, we will look at some alternative ways of testing our client application.

> If you look at the code for [Program.cs](Conqueror.Recipes.Messaging.TestingCallingHttp.Client/Program.cs), you see that we are using a `HostBuilder` (via the [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) package) instead of a plain `ServiceCollection`. This allows us to configure services from within our tests by using a trick as shown below.

Compared to the client from the recipe for [calling HTTP messages from another application](../calling-http#readme), the client in this recipe composes its message senders in the service collection instead of inline at each call site:

```cs
services.AddConquerorHttpClient();

// in a real application the server address would be loaded from some configuration
// source; the HTTP client is registered in the services so that tests can replace it
services.AddSingleton(new HttpClient { BaseAddress = new("http://localhost:5000") });

// register ready-configured handlers for the messages we call; this gives the rest of
// the application (and our tests) a single seam for how the messages are sent
services.AddSingleton<IncrementCounter.IHandler>(p =>
    p.GetRequiredService<IMessageSenders>()
        .For(IncrementCounter.T)
        .WithPipeline(pipeline => pipeline.UseDataAnnotationValidation())
        .WithTransport(b => b.UseCounterServer()));
```

This is an important design consideration for making code which calls HTTP messages testable: since **Conqueror** configures the transport at the call site, your tests can only influence how a message is sent if your application funnels that configuration through its services. Our client does this in two places: the fully configured handlers are registered in the service collection (so that tests can replace a handler wholesale), and the `UseCounterServer` transport extension ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingCallingHttp.Client/CounterServerTransportExtensions.cs)) resolves its `HttpClient` from the services (so that tests can redirect the HTTP calls to a test server):

```cs
public static IHttpMessageSender<TMessage, TResponse> UseCounterServer<TMessage, TResponse>(
    this MessageSenderBuilder<TMessage, TResponse> builder)
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    // the HTTP client is resolved from the app's services (with the server address as its
    // base address) so that tests can replace it, e.g. with a test server's client
    var httpClient = builder.ServiceProvider.GetRequiredService<HttpClient>();

    return builder.UseHttp(httpClient.BaseAddress!)
                  .WithHttpClient(httpClient)
                  .WithHeaders(h => h.Add("my-header", "my-value"));
}
```

Let's start with unit testing the `get` operation. Create a new class `GetOperationTests.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingCallingHttp.Client.Tests/GetOperationTests.cs)):

```cs
namespace Conqueror.Recipes.Messaging.TestingCallingHttp.Client.Tests;

[TestFixture]
public class GetOperationTests
{
}
```

To test the operation we need to execute our application with arguments like `get` and `inc`. We also need to be able to modify the app's services. One way to do this would be to refactor the application to provide an explicit entry point for doing this. However, as a showcase for how a console app can be tested without requiring such a refactoring, our test project contains a helper class [ProgramInvoker.cs](Conqueror.Recipes.Messaging.TestingCallingHttp.Client.Tests/ProgramInvoker.cs). This class allows executing our application with given arguments and allows configuring the app's services.

> The helper invokes the app's implicitly created `Program` class, which is `internal` due to top-level statements. Our client project makes it visible to the test project with `[assembly: InternalsVisibleTo(...)]` ([view file](Conqueror.Recipes.Messaging.TestingCallingHttp.Client/AssemblyAttributes.cs)).

Using this helper class, we can write a first test which asserts that a counter value can be fetched. For this test, we are using a unit testing approach, meaning we want to test the application in total isolation. Our application registers a `GetCounterValue.IHandler` configured with the HTTP transport. Since we don't want to make a real HTTP call during the test, we need to replace the handler in the services. The simplest way to do this is to create a mock for the generated `GetCounterValue.IHandler` interface with [NSubstitute](https://nsubstitute.github.io/) and add the mock to the services. With this, our test could look like this:

```cs
[Test]
public async Task GivenExistingCounter_WhenExecutingGetOperation_PrintsCounterValue()
{
    const string counterName = "testCounter";
    const int counterValue = 10;

    var handler = Substitute.For<GetCounterValue.IHandler>();
    handler.Handle(Arg.Any<GetCounterValue>(), Arg.Any<CancellationToken>())
           .Returns(call => call.Arg<GetCounterValue>().CounterName == counterName
                                ? new GetCounterValueResponse(CounterExists: true, counterValue)
                                : new GetCounterValueResponse(CounterExists: false, CounterValue: null));

    // this replaces the app's own registration for `GetCounterValue.IHandler`, so
    // the app uses the mock instead of sending the message via HTTP
    var output = await ProgramInvoker.Invoke(services => services.AddSingleton(handler), "get", counterName);

    Assert.That(output.Trim(), Is.EqualTo($"counter '{counterName}' value: {counterValue}"));
}
```

An alternative approach is to use a capability of **Conqueror** which allows creating a handler from a delegate. The delegate handler is registered in the app's services, and the handler registration is re-bound to a plain sender without a transport, so that the message is handled in-process:

```cs
[Test]
public async Task GivenExistingCounter_WhenExecutingGetOperationWithInProcessHandler_PrintsCounterValue()
{
    const string counterName = "testCounter";
    const int counterValue = 10;

    var output = await ProgramInvoker.Invoke(services =>
    {
        // create a handler from a delegate and re-bind the app's handler registration to a
        // plain sender without a transport, so that the message is handled in-process
        services.AddMessageHandlerDelegate(GetCounterValue.T, async (message, _, _) =>
        {
            await Task.CompletedTask;
            return message.CounterName == counterName ? new(CounterExists: true, counterValue) : new GetCounterValueResponse(CounterExists: false, CounterValue: null);
        });

        services.AddSingleton<GetCounterValue.IHandler>(p => p.GetRequiredService<IMessageSenders>().For(GetCounterValue.T));
    }, "get", counterName);

    Assert.That(output.Trim(), Is.EqualTo($"counter '{counterName}' value: {counterValue}"));
}
```

> Creating handlers from delegates like this is only recommended for testing purposes. In the application code itself, handlers should always be proper classes.

We can also write tests for error cases. Recall that a failed HTTP call surfaces as an [HttpMessageFailedOnClientException](../../../Conqueror.Transport.Http.Abstractions/Messaging/HttpMessageFailedOnClientException.cs), which we can simply throw from the mock:

```cs
[Test]
public async Task WhenExecutingGetOperationFailsWithHttpError_PrintsErrorMessage()
{
    const string counterName = "testCounter";
    const HttpStatusCode errorStatusCode = HttpStatusCode.InternalServerError;

    var handler = Substitute.For<GetCounterValue.IHandler>();
    handler.Handle(Arg.Any<GetCounterValue>(), Arg.Any<CancellationToken>())
           .ThrowsAsync(new HttpMessageFailedOnClientException("message failed")
           {
               Response = new() { StatusCode = errorStatusCode },
               MessagePayload = new GetCounterValue(counterName),
               TransportType = new(ConquerorTransportHttpConstants.TransportName, MessageTransportRole.Sender),
           });

    var output = await ProgramInvoker.Invoke(services => services.AddSingleton(handler), "get", counterName);

    Assert.That(output.Trim(), Is.EqualTo($"HTTP message failed with status code {(int)errorStatusCode}"));
}
```

Next, we will test the `inc` operation. We are going to write tests in three different ways and will discuss the advantages and disadvantages of each. Create a new class `IncOperationTests.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingCallingHttp.Client.Tests/IncOperationTests.cs)):

```cs
namespace Conqueror.Recipes.Messaging.TestingCallingHttp.Client.Tests;

[TestFixture]
public class IncOperationTests
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

    var handler = Substitute.For<IncrementCounter.IHandler>();
    handler.Handle(Arg.Any<IncrementCounter>(), Arg.Any<CancellationToken>())
           .Returns(call => call.Arg<IncrementCounter>().CounterName == counterName
                                ? new IncrementCounterResponse(incrementedCounterValue)
                                : new IncrementCounterResponse(-1));

    var output = await ProgramInvoker.Invoke(services => services.AddSingleton(handler), "inc", counterName);

    Assert.That(output.Trim(), Is.EqualTo($"incremented counter '{counterName}'; new value: {incrementedCounterValue}"));
}
```

The biggest advantage of this approach is that it is very simple. However, let's recall how the app configures the sender for the `IncrementCounter` message:

```cs
services.AddSingleton<IncrementCounter.IHandler>(p =>
    p.GetRequiredService<IMessageSenders>()
        .For(IncrementCounter.T)
        .WithPipeline(pipeline => pipeline.UseDataAnnotationValidation())
        .WithTransport(b => b.UseCounterServer()));
```

The sender validates the message in its pipeline, and the `UseCounterServer` transport extension adds a custom HTTP header. With the unit testing approach above, these aspects are not tested at all, since we completely replaced the registered `IncrementCounter.IHandler`. If we want to test these aspects, we need to do something else.

What we are going to do is to create a simple web host that is configured to use the ASP.NET Core test server. In this web server, we are going to add a message handler delegate and expose it via HTTP. Then we will replace the `HttpClient` in the client app's services with the test server's test client. With that setup, the message sender execution will go through the full HTTP invocation, allowing us to test aspects like HTTP headers as well.

> The test project is already configured with all the required dependencies. You need the [Microsoft.AspNetCore.Mvc.Testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) and [Conqueror.Transport.Http.Server.AspNetCore](https://www.nuget.org/packages/Conqueror.Transport.Http.Server.AspNetCore) packages.

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
                            services.AddRouting()
                                    .AddConquerorHttpServerAspNetCore();

                            // `AddHttpMessageHandlerDelegate` registers a handler from a delegate
                            // which is exposed via HTTP when calling `MapMessageEndpoints`
                            services.AddHttpMessageHandlerDelegate(IncrementCounter.T, async (message, _, _) =>
                            {
                                await Task.CompletedTask;
                                return message.CounterName == counterName ? new(incrementedCounterValue) : new IncrementCounterResponse(-1);
                            });
                        })
                        .Configure(app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapMessageEndpoints()))
                        .Build();

    await webHost.StartAsync();

    var testClient = webHost.GetTestClient();

    // replace the app's `HttpClient` with the test server's client, so that all
    // HTTP messages are sent to the test server
    var output = await ProgramInvoker.Invoke(services => services.AddSingleton(testClient), "inc", counterName);

    Assert.That(output.Trim(), Is.EqualTo($"incremented counter '{counterName}'; new value: {incrementedCounterValue}"));
}
```

The biggest advantage of this approach is that it tests the full client configuration. The biggest downside is that it requires more setup code (which grows as the complexity of your message senders grows, e.g. with more complex pipelines and more HTTP-specifics like custom paths).

We want to write a few more tests that use the same setup. To prevent having to include the setup boilerplate code in each test, let's extract it into a helper method (it could also be extracted into a composition-based helper class to allow re-using it across multiple test classes):

```cs
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
```

With this helper method we can write tests to assert that the message validation works, to assert that the custom HTTP header is passed correctly, and to assert what happens when execution fails:

```cs
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
```

As you can see, the tests are very concise, even though they test the integration of many aspects. Note in particular the first test: the message validation happens in the sender's pipeline on the client, so the invalid message is rejected before any HTTP request is sent at all (which the test proves by asserting that the test server never saw a request).

The final approach we are going to look at is to use the real server application in our tests. This is only possible if the server and client live in the same code base, but provides the maximum amount of integration. Our test project already references the server project, so we can write a test as follows:

```cs
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
```

> The server application uses an explicit `ServerProgram` class ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingCallingHttp.Server/ServerProgram.cs)) instead of top-level statements, so that it can be referenced unambiguously in `WebApplicationFactory<ServerProgram>` alongside the client's `Program` class.

And that concludes this recipe for testing your code which calls HTTP messages with **Conqueror**. Which of the test approaches we explored in this recipe you want to use is up to your and the requirements of the application you are building. In general, our recommendation is to **strive for the maximum amount of integration in your tests**. This means that, if possible, you should test against the real server application and use a mocked web application only for edge cases that cannot be reproduced using the server app. If your server and client applications are not part of the same code base, then use a mocked web application to test the full sender pipeline and HTTP behavior. If you value simplicity over everything else, then replacing the registered handler with a mock is the way to go.

As the next step we recommend that you explore how to [create a clean architecture with messages](../../../../../core/recipes/messaging/clean-architecture#readme). After you have finished that recipe you can take a look at the recipe for [moving from a modular monolith to a distributed system](../../../../../core/recipes/messaging/monolith-to-distributed#readme), which builds upon the concepts we just explored.

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.testing-calling-http]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
