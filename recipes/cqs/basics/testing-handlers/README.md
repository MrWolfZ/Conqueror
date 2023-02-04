# Conqueror recipe (CQS Basics): testing command and query handlers

This recipe shows how simple it is to test your command and query handlers with **Conqueror.CQS**.

The handlers we are going to test are similar to those we built in the recipe for [getting started](../getting-started#readme). If you have not yet read that recipe, we recommend you take a look before you start.

> You can see the completed code for this recipe directly in the [repository](.). We're using the [NUnit](https://nunit.org) framework, but any of the points discussed here apply to other testing frameworks as well.

The application we will be testing is managing a set of named counters. When you use **Conqueror.CQS**, your commands and queries represent the public API for your application. In code, the API of our application under test is represented with the following types:

```cs
public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);

public sealed record IncrementCounterCommand(string CounterName);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);
```

> You can of course also have commands and queries which are only used internally in your application, and are therefore not part of its public API. For this recipe we are only focusing on those that are public.

Feel free to take a look at the full code for [the query](Conqueror.Recipes.CQS.Basics.TestingHandlers/GetCounterValueQuery.cs) and [the command](Conqueror.Recipes.CQS.Basics.TestingHandlers/IncrementCounterCommand.cs). The counters are stored in an [in-memory repository](Conqueror.Recipes.CQS.Basics.TestingHandlers/CountersRepository.cs).

For testing applications written with **Conqueror.CQS**, we recommend the [black-box testing](https://en.wikipedia.org/wiki/Black-box_testing) approach. With this approach you only test the public API of your application without any knowledge about its internal implementation. As discussed above, our commands and queries are a perfect fit for this, since they represent the totality of our application's API without any unnecessary implementation details. For example, this means the repository is not tested directly, since it is an implementation detail.

> In a real application, your repository will talk to some kind of database. We still recommend to let the tests use the real repository (and therefore real database) to make them test the application behavior as closely to production as possible.

Given that we want to test the public application API, it follows naturally that we should not be testing command and query handler classes directly, but instead we should invoke handlers through their interface. This means we need to set up the handlers and their dependencies inside the tests. We'll look at two different ways for doing this.

> If you want to follow along with coding while reading this recipe, you can copy the [recipe directory](.) to your local machine and then delete all `*.cs` files from the `Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests` directory.

Let's start with testing the [GetCounterValueQuery](Conqueror.Recipes.CQS.Basics.TestingHandlers/GetCounterValueQuery.cs) by creating a new test class called [GetCounterValueQueryTests.cs](Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests/GetCounterValueQueryTests.cs):

```cs
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests;

[TestFixture]
public sealed class GetCounterValueQueryTests
{
}
```

To test the query handler we need to instantiate it. To do this we'll create a service collection manually, add all required services, and then resolve the handler from the service provider. Let's do this in our first test, which is going to verify that an exception is thrown when trying to get the value of a counter which does not exist. For naming the tests, we recommend the [given-when-then](https://martinfowler.com/bliki/GivenWhenThen.html) style.

```cs
[Test]
public async Task GivenNonExistingCounter_WhenGettingCounterValue_CounterNotFoundExceptionIsThrown()
{
    var services = new ServiceCollection();

    services.AddConquerorCQS()
            .AddTransient<GetCounterValueQueryHandler>()
            .AddSingleton<CountersRepository>()
            .FinalizeConquerorRegistrations();

    await using var serviceProvider = services.BuildServiceProvider();

    var handler = serviceProvider.GetRequiredService<IGetCounterValueQueryHandler>();

    Assert.ThrowsAsync<CounterNotFoundException>(() => handler.ExecuteQuery(new("test-counter")));
}
```

If you are following along with coding, you can now run this test, and it should succeed.

Let's add another test for getting an existing counter's value.

```cs
[Test]
public async Task GivenExistingCounter_WhenGettingCounterValue_CounterValueIsReturned()
{
    var services = new ServiceCollection();

    services.AddConquerorCQS()
            .AddTransient<GetCounterValueQueryHandler>()
            .AddSingleton<CountersRepository>()
            .FinalizeConquerorRegistrations();

    await using var serviceProvider = services.BuildServiceProvider();

    var handler = serviceProvider.GetRequiredService<IGetCounterValueQueryHandler>();
    var repository = serviceProvider.GetRequiredService<CountersRepository>();

    await repository.SetCounterValue("test-counter", 10);

    var response = await handler.ExecuteQuery(new(TestCounterName));

    Assert.That(response.CounterValue, Is.EqualTo(10));
}
```

As you can see there are a few repititions, so in accordance with the [DRY principle](https://en.wikipedia.org/wiki/Don't_repeat_yourself), we can extract a constant and method. This leads to the following complete test file:

```cs
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests;

[TestFixture]
public sealed class GetCounterValueQueryTests
{
    private const string TestCounterName = "test-counter";

    [Test]
    public async Task GivenNonExistingCounter_WhenGettingCounterValue_CounterNotFoundExceptionIsThrown()
    {
        await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IGetCounterValueQueryHandler>();

        Assert.ThrowsAsync<CounterNotFoundException>(() => handler.ExecuteQuery(new(TestCounterName)));
    }

    [Test]
    public async Task GivenExistingCounter_WhenGettingCounterValue_CounterValueIsReturned()
    {
        await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IGetCounterValueQueryHandler>();
        var repository = serviceProvider.GetRequiredService<CountersRepository>();

        await repository.SetCounterValue(TestCounterName, 10);

        var response = await handler.ExecuteQuery(new(TestCounterName));

        Assert.That(response.CounterValue, Is.EqualTo(10));
    }

    private static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection().AddConquerorCQS()
                                      .AddTransient<GetCounterValueQueryHandler>()
                                      .AddSingleton<CountersRepository>()
                                      .FinalizeConquerorRegistrations()
                                      .BuildServiceProvider();
    }
}
```

This concludes the tests for the [GetCounterValueQuery](Conqueror.Recipes.CQS.Basics.TestingHandlers/GetCounterValueQuery.cs). We used the approach of creating a service provider with the minimal services required for our tests. This works well, but adds a little bit of noise to the tests. Next, we are going to write tests for [IncrementCounterCommand](Conqueror.Recipes.CQS.Basics.TestingHandlers/IncrementCounterCommand.cs) using a slightly different approach.

The [IncrementCounterCommand](Conqueror.Recipes.CQS.Basics.TestingHandlers/IncrementCounterCommand.cs) contains something which is quite common for commands: a side-effect. Side-effects are things that happen as the results of calling a command, but are not directly visible in the command's response. In this case the side-effect is sending a notification to an administrator whenever a counter is incremented beyond a threshold of 1000 (through the [IAdminNotificationService](Conqueror.Recipes.CQS.Basics.TestingHandlers/IAdminNotificationService.cs)), which is admittedly not very realistic for such a simple application, but it serves well to illustrate how to deal with such side-effects during testing.

> Note that queries should never have side-effects, they should only return some data. This is important to support certain advanced patterns like caching.

In contrast to testing our query above, we're going to address the test setup in a base class that our command test class can inherit from. This allows the test class to fully focus on the tests themselves without a lot of boilerplate. Let's create a new file `TestBase.cs` and add the following content:

```cs
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests;

public abstract class TestBase : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    protected TestBase()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }

    protected T Resolve<T>()
        where T : notnull => serviceProvider.GetRequiredService<T>();
}
```

There are a few things to note here. We are creating a new service collection and create the service provider in the class's constructor. There is also a call to `AddApplicationServices`, which is a method we haven't seen before. This method comes from [ServiceCollectionExtensions.cs](Conqueror.Recipes.CQS.Basics.TestingHandlers/ServiceCollectionExtensions.cs) and registers all services contained in its project. This is a recommended practice for modular system design (there is an [advanced recipe](../../advanced/clean-architecture#readme) which explains more about this).

The class implements `IDisposable` and disposes the service provider. Most testing frameworks will automatically dispose a test class after the test run is finished. For some testing frameworks there is another aspect you need to be careful of: they might re-use the same class instance for multiple tests. This could cause undesired side-effects, and therefore we recommend to configure your test framework to create a new class instance for each test. Since we are using [NUnit](https://nunit.org) in this recipe, we can do this with an assembly attribute as seen in [AssemblyAttributes](Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests/AssemblyAttributes.cs).

Now we can use this test base for our command tests. Create a new test class called [IncrementCounterCommandTests.cs](Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests/IncrementCounterCommandTests.cs) with some helper contants and properties:

```cs
using NUnit.Framework;

namespace Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests;

[TestFixture]
public sealed class IncrementCounterCommandTests : TestBase
{
    private const string TestCounterName = "test-counter";

    private IIncrementCounterCommandHandler Handler => Resolve<IIncrementCounterCommandHandler>();

    private CountersRepository CountersRepository => Resolve<CountersRepository>();
}
```

The `Handler` and `CountersRepository` properties allow accessing the handler and repository conveniently in each test.

Our first test is going to validate what happens when we increment a non-existing counter:

```cs
[Test]
public async Task GivenNonExistingCounter_WhenIncrementingCounter_CounterIsCreatedAndInitialValueIsReturned()
{
    var response = await Handler.ExecuteCommand(new(TestCounterName));

    var storedCounterValue = await CountersRepository.GetCounterValue(TestCounterName);

    Assert.That(storedCounterValue, Is.EqualTo(1).And.EqualTo(response.NewCounterValue));
}
```

As you can see, the test is short and clear, thanks to our test infrastructure.

> We validate the result of the command by checking the repository directly, which is a small violation of the black-box testing approach. The alternative would be to fetch the counter's value with the `GetCounterValueQuery`, but this adds a dependency to this query to the tests for the command. A third option would be to not test the command and query separately but instead test the whole counter "domain" together by creating more end-to-end test cases like `GivenNonExistingCounter_WhenIncrementingCounter_CounterIsCreatedAndValueCanBeFetchedByQuery`. Which of these approaches you choose is up to you to decide, they all have different trade-offs.

Let's add another test for incrementing an existing counter:

```cs
[Test]
public async Task GivenExistingCounter_WhenIncrementingCounter_CounterIsIncrementedAndValueIsReturned()
{
    await CountersRepository.SetCounterValue(TestCounterName, 10);

    var response = await Handler.ExecuteCommand(new(TestCounterName));

    var storedCounterValue = await CountersRepository.GetCounterValue(TestCounterName);

    Assert.That(storedCounterValue, Is.EqualTo(11).And.EqualTo(response.NewCounterValue));
}
```

Another short and clear test.

Finally we need to test the side-effect. We want to verify that a notification is sent when a counter is incremented beyond the threshold of 1000. Unfortunately there is no easy way to verify this with the existing production code, so we are going to use a [mock object](https://en.wikipedia.org/wiki/Mock_object). There is a variety of libraries for this out there. For this recipe we'll use [Moq](https://github.com/moq/moq4).

```sh
dotnet add package moq
```

Since we resolve the handler from the service provider for testing, we need to register the mock object in the service provider. Here is the full [TestBase.cs](Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests/TestBase.cs), where we replace the service descriptor for `IAdminNotificationService` with a mock object:

```cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Conqueror.Recipes.CQS.Basics.TestingHandlers.Tests;

public abstract class TestBase : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    protected TestBase()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        services.Replace(ServiceDescriptor.Singleton(AdminNotificationServiceMock.Object));

        serviceProvider = services.BuildServiceProvider();
    }

    protected Mock<IAdminNotificationService> AdminNotificationServiceMock { get; } = new();

    public void Dispose()
    {
        serviceProvider.Dispose();
    }

    protected T Resolve<T>()
        where T : notnull => serviceProvider.GetRequiredService<T>();
}
```

> Our recommended approach is to mock all side-effectul dependencies inside the `TestBase` to ensure that you never accidentally trigger a real side-effect during testing. However, it is possible to delegate the creation of the mocks to the concrete test class by creating a virtual `ConfigureServices` method and creating the service provider lazily. This is left as an exercise for the reader.

With this change we can now write a test to verify that the notification is sent:

```cs
[Test]
public async Task GivenExistingCounter_WhenIncrementingCounterAboveThreshold_AdminNotificationIsSent()
{
    await CountersRepository.SetCounterValue(TestCounterName, 999);

    _ = await Handler.ExecuteCommand(new(TestCounterName));

    AdminNotificationServiceMock.Verify(s => s.SendCounterIncrementedBeyondThresholdNotification(TestCounterName));
}
```

Still short and clear thanks to our `TestBase`.

For completeness, let's also add a test for the negative case, i.e. that no notification is sent as long as a counter is incremented below the threshold.

```cs
[Test]
public async Task GivenExistingCounter_WhenIncrementingCounterBelowThreshold_NoAdminNotificationIsSent()
{
    await CountersRepository.SetCounterValue(TestCounterName, 10);

    _ = await Handler.ExecuteCommand(new(TestCounterName));

    AdminNotificationServiceMock.Verify(s => s.SendCounterIncrementedBeyondThresholdNotification(TestCounterName), Times.Never);
}
```

And that concludes this recipe for testing command and query handlers with **Conqueror.CQS**. In summary, we recommend the following when testing handlers:

- always test handlers through their public interface resolved from a service provider
- focus on testing the public API (i.e. commands and queries) of your application instead of testing implementation details
- mock dependencies which cause side-effects (e.g. sending a notification), but do not mock your storage layer (i.e. the database)
- consolidate common setup logic into a base class

As the next step you can explore how to [address cross-cutting concerns like validation](../solving-cross-cutting-concerns#readme) or take a look at how to [expose your command and queries via HTTP](../../advanced/exposing-via-http#readme) and [how to test them](../../advanced/testing-http#readme) (the linked recipe contains an implementation of [TestBase](../../advanced/testing-http/Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests/TestBase.cs) which creates a fully configured HTTP test server).

Or head over to our [other recipes](../../../../README.md#recipes) for more guidance on different topics.
