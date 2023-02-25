# Conqueror recipe (CQS Advanced): testing HTTP commands and queries

This recipe shows how simple it is to test your HTTP commands and queries with **Conqueror.CQS**.

This is an advanced recipe which builds upon the concepts introduced in the [recipes about CQS basics](../../../../../..#cqs-basics) as well as the recipe for [exposing commands and queries via HTTP](../exposing-via-http#readme). If you have not yet read those recipes, we recommend you take a look at them before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/testing-http) and open the solution file in your IDE (note that you need to have [.NET 6 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/testing-http).

The application, for which we will be adding tests for HTTP commands and queries, is managing a set of named counters. In code, the API of our application is represented with the following types:

```cs
[HttpCommand(Version = "v1")]
public sealed record IncrementCounterCommand([Required] string CounterName);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

[HttpQuery(Version = "v1")]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);
```

Feel free to take a look at the full code for [the query](Conqueror.Recipes.CQS.Advanced.TestingHttp/GetCounterValueQuery.cs) and [the command](Conqueror.Recipes.CQS.Advanced.TestingHttp/IncrementCounterCommand.cs). The counters are stored in an [in-memory repository](Conqueror.Recipes.CQS.Advanced.TestingHttp/CountersRepository.cs).

You may recall our advice from recipe for [testing command and query handlers](../../basics/testing-handlers#readme) that we recommend using [black-box testing](https://en.wikipedia.org/wiki/Black-box_testing) and focusing your tests on the public API of your aplication without any knowledge about the internal implementation. This advice still holds true for HTTP commands and queries, which means they should be tested by calling them via HTTP.

Fortunately, ASP.NET Core provides excellent tools for [integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests) with the [Microsoft.AspNetCore.Mvc.Testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) package. We'll make use of those tools in the tests for our application to make them as simple as they can be (the package is already installed in our test project).

We will start by writing tests for the [GetCounterValueQuery](Conqueror.Recipes.CQS.Advanced.TestingHttp/GetCounterValueQuery.cs). Create a new file `GetCounterValueQueryTests.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests/GetCounterValueQueryTests.cs)) in the test project:

```cs
namespace Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests;

[TestFixture]
public sealed class GetCounterValueQueryTests
{
}
```

To be able to call our query via HTTP we need to launch our application. This can be done using [WebApplicationFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1). Let's add some setup code to launch the app and create an HTTP test client:

```cs
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
}
```

With this we are now ready to to write our first test. Let's write a test which asserts that we can successfully get the value of an existing counter:

```cs
[Test]
public async Task GivenExistingCounter_WhenGettingCounterValue_ThenCounterValueIsReturned()
{
    const string testCounterName = "testCounter";
    const int testCounterValue = 10;

    await applicationFactory.Services.GetRequiredService<CountersRepository>().SetCounterValue(testCounterName, testCounterValue);

    var response = await httpTestClient.GetFromJsonAsync<GetCounterValueQueryResponse>($"/api/v1/queries/getCounterValue?counterName={testCounterName}");

    Assert.That(response, Is.Not.Null);
    Assert.That(response!.CounterExists, Is.True);
    Assert.That(response.CounterValue, Is.EqualTo(testCounterValue));
}
```

Next, we can add a test to assert that validation works:

```cs
[Test]
public async Task WhenExecutingInvalidQuery_RequestFailsWithBadRequest()
{
    // omit counterName parameter to make query invalid
    var response = await httpTestClient.GetAsync("/api/v1/queries/getCounterValue");

    Assert.That(response, Is.Not.Null);
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
}
```

> Writing a test for getting the value of a non-existing counter is left as an exercise for the reader.

The tests above are normal HTTP tests just like you might write them for your other controllers. One downside to writing tests like this for HTTP commands and queries is that you need to manually construct the target URI in the test, which makes your tests brittle. However, **Conqueror.CQS** provides a way to create HTTP clients for your commands and queries which simplify their usage. The full details of this are discussed in the recipe for [calling HTTP commands and queries](../calling-http#readme), therefore in this recipe we will limit ourselves to the aspects relevant for testing. Let's install the HTTP client package into the test project:

```sh
dotnet add Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests package Conqueror.CQS.Transport.Http.Client
```

We will explore using **Conqueror.CQS** HTTP clients in the tests for the [IncrementCounterCommand](Conqueror.Recipes.CQS.Advanced.TestingHttp/IncrementCounterCommand.cs). But before we start with writing those tests we can simplify our test setup a bit by extracting the web application bootstrapping code into a base class called `TestBase`. Create a new file `TestBase.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests/TestBase.cs)):

```cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests;

public abstract class TestBase : IDisposable
{
    private readonly WebApplicationFactory<Program> applicationFactory;
    private readonly ServiceProvider clientServices;
    private readonly HttpClient httpTestClient;

    protected TestBase()
    {
        applicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // configure tests to produce logs; this is very useful to debug failing tests
            builder.ConfigureLogging(o => o.ClearProviders()
                                           .AddSimpleConsole()
                                           .SetMinimumLevel(LogLevel.Information));
        });

        httpTestClient = applicationFactory.CreateClient(new() { AllowAutoRedirect = false });

        // create a dedicated service provider for resolving command and query clients
        // to prevent interference with other services from the actual application; we also
        // configure the client services to use our test server's HTTP client
        clientServices = new ServiceCollection().AddConquerorCQSHttpClientServices(o => o.UseHttpClient(httpTestClient))
                                                .BuildServiceProvider();
    }

    public void Dispose()
    {
        clientServices.Dispose();
        httpTestClient.Dispose();
        applicationFactory.Dispose();
    }

    protected T ResolveOnServer<T>()
        where T : notnull => applicationFactory.Services.GetRequiredService<T>();

    protected T ResolveOnClient<T>()
        where T : notnull => clientServices.GetRequiredService<T>();
}
```

Now we can create the test class for our command in a new file `IncrementCounterCommandTests.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests/IncrementCounterCommandTests.cs)):

```cs
namespace Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests;

[TestFixture]
public sealed class IncrementCounterCommandTests : TestBase
{
}
```

To be able to call our HTTP command, we need to create a client for it. To make the client usable in all tests, we create a property as follows:

```cs
private IIncrementCounterCommandHandler CommandClient =>
    Resolve<ICommandClientFactory>().CreateCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(new("http://localhost")));
```

> The base address we pass to `UseHttp` does not matter, since we configured the client services to use our test server's HTTP client.

As you can see, the client implements exactly the same interface as the command handler (`IIncrementCounterCommandHandler`), but is configured to use the HTTP transport (creating a client for queries works the same way, except using the `IQueryClientFactory`). Now we can add a first test, which asserts that an existing counter can be incremented:

```cs
[Test]
public async Task GivenExistingCounter_WhenIncrementingCounter_CounterIsIncrementedAndNewValueIsReturned()
{
    const string testCounterName = "testCounter";
    const int initialCounterValue = 10;
    const int expectedCounterValue = 11;

    var countersRepository = Resolve<CountersRepository>();

    await countersRepository.SetCounterValue(testCounterName, initialCounterValue);

    var response = await CommandClient.ExecuteCommand(new(testCounterName));

    var storedCounterValue = await countersRepository.GetCounterValue(testCounterName);

    Assert.That(response.NewCounterValue, Is.EqualTo(expectedCounterValue).And.EqualTo(storedCounterValue));
}
```

What's awesome about writing tests like this is that they look exactly like tests for non-HTTP commands and queries. The only difference is how the handler interface is resolved. This allows you to fully focus on testing your business logic instead of spending valuable time on HTTP details.

> Using **Conqueror.CQS** HTTP clients like this is only supported for commands and queries that are exposed with the `HttpCommand` and `HttpQuery` attributes. If you are writing your own controllers you need to use the plain HTTP client like we did above when testing the query.

This HTTP client abstraction works perfectly for successful commands and queries, but breaks down a bit when dealing with failures. Let's write a test to assert that validation works for the command:

```cs
[Test]
public void WhenExecutingInvalidCommand_ExecutionFailsWithValidationError()
{
    var exception = Assert.ThrowsAsync<HttpCommandFailedException>(() => CommandClient.ExecuteCommand(new(string.Empty)));

    Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
}
```

We need to handle a specific exception type which describes the HTTP failure ([HttpCommandFailedException](../../../../src/Conqueror.CQS.Transport.Http.Abstractions/HttpCommandFailedException.cs) for commands and [HttpQueryFailedException](../../../../src/Conqueror.CQS.Transport.Http.Abstractions/HttpQueryFailedException.cs) for queries). We believe that this is an acceptable trade-off to keep the majority of your tests simple and HTTP-agnostic.

> There _is_ a way to map HTTP failures back to normal exceptions, but it is tricky to get fully right and it is specific to your application. In the recipe for [calling HTTP commands and queries](../calling-http#readme) we discussed how you can use middlewares in your HTTP clients. What you could do is to write a middleware that catches the HTTP exceptions and transforms them back into normal exceptions based on the status code (e.g. status code `401` could be mapped to `ValidationException`). This would allow your tests to be fully agnostic towards HTTP. However, it also increases the overall complexity of your tests and it may be difficult to create a perfect mapping of HTTP failures to normal exceptions. Whether you want to follow this approach or not is up to you. Writing a middleware to do this is left as an exercise for the reader.

One last thing to talk about is dealing with cross-cutting concerns like authentication during testing. **Conqueror.CQS** provides a set of dedicated recipes for [addressing specific cross-cutting concerns](../../../../README.md#cqs-cross-cutting-concerns). Each of these recipes includes a discussion about testing, and, where applicable, also talks about HTTP-specifics (for example, in the recipe about [authentication and authorization](../../cross-cutting-concerns/auth#readme)).

And that concludes this recipe for testing your HTTP commands and queries with **Conqueror.CQS**. In summary, we recommend the following:

- test HTTP commands and queries by calling them with an HTTP test client as an integration test
- use **Conqueror.CQS** command and query clients configured for HTTP where possible
- use custom HTTP calls to test advanced HTTP scenarios for which the command and query clients are not sufficient (e.g. if you create your own controllers)
- consolidate common setup logic into a base class

As the next step we recommend that you explore how to [call HTTP commands and queries](../calling-http#readme) from another application or how to [create a clean architecture with commands and queries](../clean-architecture#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.cqs.advanced.testing-http]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
