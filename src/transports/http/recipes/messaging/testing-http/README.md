# Conqueror recipe (Messaging): testing HTTP messages

This recipe shows how simple it is to test your HTTP messages with **Conqueror**.

This is an advanced recipe which builds upon the concepts introduced in the [recipes about messaging basics](../../../../../..#messaging-basics) as well as the recipe for [exposing messages via HTTP](../exposing-via-http#readme). If you have not yet read those recipes, we recommend you take a look at them before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/transports/http/recipes/messaging/testing-http) and open the solution file in your IDE (note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/transports/http/recipes/messaging/testing-http).

The application, for which we will be adding tests for HTTP messages, is managing a set of named counters. In code, the API of our application is represented with the following types:

```cs
[HttpMessage<IncrementCounterResponse>(Version = "v1")]
public partial record IncrementCounter(string CounterName);

public record IncrementCounterResponse(int NewCounterValue);

[HttpMessage<GetCounterValueResponse>(HttpMethod = "GET", Version = "v1")]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);
```

Feel free to take a look at the full code for [incrementing a counter](.completed/Conqueror.Recipes.Messaging.TestingHttp/IncrementCounter.cs) and [getting a counter's value](.completed/Conqueror.Recipes.Messaging.TestingHttp/GetCounterValue.cs). The counters are stored in an [in-memory repository](.completed/Conqueror.Recipes.Messaging.TestingHttp/CountersRepository.cs). The application is already [set up to expose these messages via HTTP](Conqueror.Recipes.Messaging.TestingHttp/Program.cs).

You may recall our advice from the recipe for [testing message handlers](../../../../../core/recipes/messaging/testing-handlers#readme) that we recommend using [black-box testing](https://en.wikipedia.org/wiki/Black-box_testing) and focusing your tests on the public API of your application without any knowledge about the internal implementation. This advice still holds true for HTTP messages, which means they should be tested by calling them via HTTP.

Fortunately, ASP.NET Core provides excellent tools for [integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests) with the [Microsoft.AspNetCore.Mvc.Testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) package. We'll make use of those tools in the tests for our application to make them as simple as they can be (the package is already installed in our test project).

We will start by writing tests for the [GetCounterValue](.completed/Conqueror.Recipes.Messaging.TestingHttp/GetCounterValue.cs) message. Create a new file `GetCounterValueTests.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingHttp.Tests/GetCounterValueTests.cs)) in the test project:

```cs
namespace Conqueror.Recipes.Messaging.TestingHttp.Tests;

[TestFixture]
public class GetCounterValueTests
{
}
```

To be able to call our message via HTTP we need to launch our application. This can be done using [WebApplicationFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1). Since we will need this setup in every test class, we place it in a small helper right away. We use a composition-based `TestHost` (the same approach as in the [testing message handlers](../../../../../core/recipes/messaging/testing-handlers#readme) recipe), which launches the app and exposes an HTTP client for talking to it. Create a new file `TestHost.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingHttp.Tests/TestHost.cs)):

```cs
namespace Conqueror.Recipes.Messaging.TestingHttp.Tests;

internal sealed class TestHost : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> applicationFactory = new();

    private TestHost()
    {
        HttpTestClient = applicationFactory.CreateClient();
    }

    public HttpClient HttpTestClient { get; }

    public static TestHost Create() => new();

    public T ResolveOnServer<T>()
        where T : notnull => applicationFactory.Services.GetRequiredService<T>();

    public async ValueTask DisposeAsync() => await applicationFactory.DisposeAsync();
}
```

> `WebApplicationFactory<Program>` needs access to the application's `Program` class. Since top-level statements generate an internal `Program`, our application project makes it visible to the test project with `[assembly: InternalsVisibleTo(...)]` ([view file](.completed/Conqueror.Recipes.Messaging.TestingHttp/AssemblyAttributes.cs)).

The `ResolveOnServer` method allows us to resolve services from the running application, for example to seed some data before making a request. With this we are now ready to write our first test. Let's write a test which asserts that we can successfully get the value of an existing counter:

```cs
[TestFixture]
public class GetCounterValueTests
{
    private const string TestCounterName = "test-counter";

    [Test]
    public async Task GivenExistingCounter_WhenGettingCounterValue_ThenCounterValueIsReturned()
    {
        await using var host = TestHost.Create();

        const int counterValue = 10;

        await host.ResolveOnServer<CountersRepository>().SetCounterValue(TestCounterName, counterValue);

        var response = await host.HttpTestClient.GetFromJsonAsync<GetCounterValueResponse>(
            $"/api/v1/getCounterValue?counterName={TestCounterName}");

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.CounterExists, Is.True);
        Assert.That(response.CounterValue, Is.EqualTo(counterValue));
    }
}
```

Recall from the [exposing messages via HTTP](../exposing-via-http#readme) recipe that with **Conqueror** we signal success or failure through the response object instead of through HTTP status codes (that is what the `CounterExists` property on our `GetCounterValueResponse` is for). Let's add a test which asserts that getting a non-existing counter is reported correctly in the response:

```cs
[Test]
public async Task GivenNonExistingCounter_WhenGettingCounterValue_ThenResponseIndicatesCounterDoesNotExist()
{
    await using var host = TestHost.Create();

    var response = await host.HttpTestClient.GetFromJsonAsync<GetCounterValueResponse>(
        $"/api/v1/getCounterValue?counterName={TestCounterName}");

    Assert.That(response, Is.Not.Null);
    Assert.That(response!.CounterExists, Is.False);
    Assert.That(response.CounterValue, Is.Null);
}
```

The tests above are normal HTTP tests just like you might write them for any other HTTP endpoint. One downside to writing tests like this is that you need to manually construct the target URI in the test, which makes your tests brittle. However, **Conqueror** provides a way to call your HTTP messages through a strongly-typed sender which simplifies their usage. The full details of this are discussed in the recipe for [calling HTTP messages](../calling-http#readme), therefore in this recipe we will limit ourselves to the aspects relevant for testing. Let's install the HTTP client package into the test project:

```sh
dotnet add Conqueror.Recipes.Messaging.TestingHttp.Tests package Conqueror.Transport.Http.Client
```

We will explore using the **Conqueror** sender in the tests for the [IncrementCounter](.completed/Conqueror.Recipes.Messaging.TestingHttp/IncrementCounter.cs) message. To send a message via HTTP we need an `IMessageSenders` instance which is configured to talk to our test server. Let's extend our `TestHost` with a dedicated service provider for the client side:

```diff
  namespace Conqueror.Recipes.Messaging.TestingHttp.Tests;

  internal sealed class TestHost : IAsyncDisposable
  {
      private readonly WebApplicationFactory<Program> applicationFactory = new();
+     private readonly ServiceProvider clientServices;

      private TestHost()
      {
          HttpTestClient = applicationFactory.CreateClient();
+
+         // create a dedicated service provider for resolving message senders to prevent
+         // interference with the services of the actual application
+         clientServices = new ServiceCollection().AddConquerorHttpClient().BuildServiceProvider();
      }

      public HttpClient HttpTestClient { get; }

+     public IMessageSenders MessageSenders => clientServices.GetRequiredService<IMessageSenders>();

      public static TestHost Create() => new();

      public T ResolveOnServer<T>()
          where T : notnull => applicationFactory.Services.GetRequiredService<T>();

-     public async ValueTask DisposeAsync() => await applicationFactory.DisposeAsync();
+     public async ValueTask DisposeAsync()
+     {
+         await clientServices.DisposeAsync();
+         HttpTestClient.Dispose();
+         await applicationFactory.DisposeAsync();
+     }
  }
```

Now we can create the test class for our message in a new file `IncrementCounterTests.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingHttp.Tests/IncrementCounterTests.cs)) and add a test which asserts that an existing counter can be incremented:

```cs
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
}
```

The only HTTP-specific part is `WithTransport(b => b.UseHttp(...).WithHttpClient(host.HttpTestClient))`, which tells the sender to call the message over HTTP using our test server's HTTP client. Aside from that, the call looks exactly like calling the handler in-process. This allows you to fully focus on testing your business logic instead of spending valuable time on HTTP details.

> The base address we pass to `UseHttp` does not matter here, since `WithHttpClient` provides the test server's HTTP client which already has its own base address configured.

> Calling messages through the sender like this is only supported for messages that are exposed with the `HttpMessage` attribute. If you are writing your own custom endpoints you need to use the plain HTTP client like we did above when testing the `GetCounterValue` message.

This strongly-typed sender works perfectly for successful messages, but it needs a little more care when dealing with failures. If a message fails on the server, the HTTP call returns an unsuccessful status code, which the sender surfaces as an [HttpMessageFailedOnClientException](../../../Conqueror.Transport.Http.Abstractions/Messaging/HttpMessageFailedOnClientException.cs). The exception exposes the `StatusCode` so that you can assert on it. Our application enforces a business rule that a counter cannot be incremented beyond the value `1000`, in which case the handler throws a `CounterValueLimitReachedException` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingHttp/IncrementCounter.cs)). By default, ASP.NET Core returns status code `500` for unhandled exceptions, so we can write a test for this failure as follows:

```cs
[Test]
public async Task GivenCounterAtValueLimit_WhenIncrementingCounter_ThenMessageFailsWithInternalServerError()
{
    await using var host = TestHost.Create();

    await host.ResolveOnServer<CountersRepository>().SetCounterValue(TestCounterName, 1000);

    var exception = Assert.ThrowsAsync<HttpMessageFailedOnClientException>(() =>
        host.MessageSenders.For(IncrementCounter.T)
            .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(host.HttpTestClient))
            .Handle(new(TestCounterName)));

    Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
}
```

We need to handle a specific exception type which describes the HTTP failure instead of the domain exception the handler throws. We believe that this is an acceptable trade-off to keep the majority of your tests simple and HTTP-agnostic.

> There _is_ a way to map HTTP failures back to normal exceptions, but it is tricky to get fully right and it is specific to your application. What you could do is to write a middleware for the sender's pipeline which catches the HTTP exceptions and transforms them back into normal exceptions based on the status code. This would allow your tests to be fully agnostic towards HTTP. However, it also increases the overall complexity of your tests and it may be difficult to create a perfect mapping of HTTP failures to normal exceptions. Whether you want to follow this approach or not is up to you. Writing a middleware to do this is left as an exercise for the reader.

> Note that **Conqueror** does not perform any data annotation validation on incoming HTTP requests. Validation is a cross-cutting concern which is best solved with a middleware in the message pipeline, so that it also takes place when your messages are executed in-process or via a different transport. Take a look at the recipe for [solving cross-cutting concerns](../../../../../core/recipes/messaging/solving-cross-cutting-concerns#readme) to see how that works, including how to test it.

And that concludes this recipe for testing your HTTP messages with **Conqueror**. In summary, we recommend the following:

- test HTTP messages by calling them with an HTTP test client as an integration test
- use the **Conqueror** message sender configured for HTTP where possible to keep your tests HTTP-agnostic
- use plain HTTP calls to test advanced HTTP scenarios for which the sender is not sufficient
- consolidate common setup logic into a composition-based `TestHost`

As the next step we recommend that you explore how to [call HTTP messages](../calling-http#readme) from another application or how to [create a clean architecture with messages](../../../../../core/recipes/messaging/clean-architecture#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.testing-http]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
