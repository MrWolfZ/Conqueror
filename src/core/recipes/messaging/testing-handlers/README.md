# Conqueror recipe (Messaging): testing handlers

This recipe shows how simple it is to test your message handlers with **Conqueror**.

The handlers we are going to test are similar to those we built in the recipe for [getting started](../getting-started#readme). If you have not yet read that recipe, we recommend you take a look before you start with this one.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/messaging/testing-handlers) and open the solution file in your IDE (note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/messaging/testing-handlers).

The application we will be testing manages a set of named counters. When you use **Conqueror** messaging, your messages represent the public API for your application. In code, the API of our application under test is represented with the following types:

```cs
[Message<GetCounterValueResponse>]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(int CounterValue);

[Message<IncrementCounterResponse>]
public partial record IncrementCounter(string CounterName);

public record IncrementCounterResponse(int NewCounterValue);
```

> You can of course also have messages which are only used internally in your application, and are therefore not part of its public API. For this recipe we are only focusing on those that are public.

Feel free to take a look at the full code for [GetCounterValue](Conqueror.Recipes.Messaging.TestingHandlers/GetCounterValue.cs) and [IncrementCounter](Conqueror.Recipes.Messaging.TestingHandlers/IncrementCounter.cs). The counters are stored in an [in-memory repository](Conqueror.Recipes.Messaging.TestingHandlers/CountersRepository.cs).

For testing applications written with **Conqueror**, we recommend the [black-box testing](https://en.wikipedia.org/wiki/Black-box_testing) approach. With this approach you only test the public API of your application without any knowledge about its internal implementation. As discussed above, our messages are a perfect fit for this, since they represent the totality of our application's API without any unnecessary implementation details. For example, this means the repository is not tested directly, since it is an implementation detail.

> In a real application, your repository will talk to some kind of database. We still recommend to let the tests use the real repository (and therefore real database) to make them test the application behavior as closely to production as possible. You can use techniques like [test containers](https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/) to achieve that.

Given that we want to test the public application API, it follows naturally that we should not be testing message handler classes directly, but instead we should invoke handlers through their interface. This means we need to set up the handlers and their dependencies inside the tests. We'll look at two different ways for doing this.

> We're using the [NUnit](https://nunit.org) framework in this recipe, but any of the points discussed here apply to any other testing framework as well.

Let's start by creating a new test project and adding the dependencies (if you prefer you can of course create the project via your IDE).

```sh
dotnet new nunit -n Conqueror.Recipes.Messaging.TestingHandlers.Tests

# add a reference to the implementation project
dotnet add Conqueror.Recipes.Messaging.TestingHandlers.Tests reference Conqueror.Recipes.Messaging.TestingHandlers

# add the NSubstitute package for creating mock objects
dotnet add Conqueror.Recipes.Messaging.TestingHandlers.Tests package NSubstitute

# add the new project to the solution
dotnet sln Conqueror.Recipes.Messaging.TestingHandlers.sln add Conqueror.Recipes.Messaging.TestingHandlers.Tests
```

Now we can start testing [GetCounterValue](Conqueror.Recipes.Messaging.TestingHandlers/GetCounterValue.cs) by creating a new test class called `GetCounterValueTests.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingHandlers.Tests/GetCounterValueTests.cs)):

```cs
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.Messaging.TestingHandlers.Tests;

[TestFixture]
public class GetCounterValueTests
{
}
```

To test the message handler we need to instantiate it. To do this we'll create a service collection manually, add all required services, and then resolve the handler from the service provider. Let's do this in our first test, which is going to verify that an exception is thrown when trying to get the value of a counter which does not exist. For naming the tests, we recommend the [given-when-then](https://martinfowler.com/bliki/GivenWhenThen.html) style.

```cs
[Test]
public async Task GivenNonExistingCounter_WhenGettingCounterValue_CounterNotFoundExceptionIsThrown()
{
    var services = new ServiceCollection();

    services.AddMessageHandler<GetCounterValueHandler>()
            .AddSingleton<CountersRepository>();

    await using var serviceProvider = services.BuildServiceProvider();

    var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(GetCounterValue.T);

    Assert.ThrowsAsync<CounterNotFoundException>(() => handler.Handle(new("test-counter")));
}
```

If you are following along with coding, you can now run this test, and it should succeed.

Let's add another test for getting an existing counter's value.

```cs
[Test]
public async Task GivenExistingCounter_WhenGettingCounterValue_CounterValueIsReturned()
{
    var services = new ServiceCollection();

    services.AddMessageHandler<GetCounterValueHandler>()
            .AddSingleton<CountersRepository>();

    await using var serviceProvider = services.BuildServiceProvider();

    var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(GetCounterValue.T);
    var repository = serviceProvider.GetRequiredService<CountersRepository>();

    await repository.SetCounterValue("test-counter", 10);

    var response = await handler.Handle(new("test-counter"));

    Assert.That(response.CounterValue, Is.EqualTo(10));
}
```

As you can see there are a few repetitions, so in accordance with the [DRY principle](https://en.wikipedia.org/wiki/Don't_repeat_yourself), we can extract a constant and method:

```diff
[TestFixture]
public class GetCounterValueTests
{
+   private const string TestCounterName = "test-counter";
+
    [Test]
    public async Task GivenNonExistingCounter_WhenGettingCounterValue_CounterNotFoundExceptionIsThrown()
    {
-       var services = new ServiceCollection();
-
-       services.AddMessageHandler<GetCounterValueHandler>()
-               .AddSingleton<CountersRepository>();
-
-       await using var serviceProvider = services.BuildServiceProvider();
+       await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(GetCounterValue.T);

-       Assert.ThrowsAsync<CounterNotFoundException>(() => handler.Handle(new("test-counter")));
+       Assert.ThrowsAsync<CounterNotFoundException>(() => handler.Handle(new(TestCounterName)));
    }

    [Test]
    public async Task GivenExistingCounter_WhenGettingCounterValue_CounterValueIsReturned()
    {
-       var services = new ServiceCollection();
-
-       services.AddMessageHandler<GetCounterValueHandler>()
-               .AddSingleton<CountersRepository>();
-
-       await using var serviceProvider = services.BuildServiceProvider();
+       await using var serviceProvider = BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(GetCounterValue.T);
        var repository = serviceProvider.GetRequiredService<CountersRepository>();

-       await repository.SetCounterValue("test-counter", 10);
+       await repository.SetCounterValue(TestCounterName, 10);

-       var response = await handler.Handle(new("test-counter"));
+       var response = await handler.Handle(new(TestCounterName));

        Assert.That(response.CounterValue, Is.EqualTo(10));
    }
+
+   private static ServiceProvider BuildServiceProvider()
+   {
+       return new ServiceCollection().AddMessageHandler<GetCounterValueHandler>()
+                                     .AddSingleton<CountersRepository>()
+                                     .BuildServiceProvider();
+   }
}
```

This concludes the tests for [GetCounterValue](Conqueror.Recipes.Messaging.TestingHandlers/GetCounterValue.cs). We used the approach of creating a service provider with the minimal services required for our tests. This works well, but adds a little bit of noise to the tests. Next, we are going to write tests for [IncrementCounter](Conqueror.Recipes.Messaging.TestingHandlers/IncrementCounter.cs) using a slightly different approach.

The [IncrementCounter](Conqueror.Recipes.Messaging.TestingHandlers/IncrementCounter.cs) handler contains something which is quite common: a side-effect. Side-effects are things that happen as the results of calling a handler, but are not directly visible in the handler's response. In this case the side-effect is sending a notification to an administrator whenever a counter is incremented beyond a threshold of 1000 (through the [IAdminNotificationService](Conqueror.Recipes.Messaging.TestingHandlers/IAdminNotificationService.cs)), which is admittedly not very realistic for such a simple application, but it serves well to illustrate how to deal with such side-effects during testing.

In contrast to testing our `GetCounterValue` handler above, we're going to address the test setup in a base class that our handler's test class can inherit from. This allows the test class to fully focus on the tests themselves without a lot of boilerplate. Let's create a new file `TestBase.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingHandlers.Tests/TestBase.cs)) and add the following content:

```cs
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.Messaging.TestingHandlers.Tests;

public abstract class TestBase
{
    private readonly ServiceProvider serviceProvider;

    protected TestBase()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        serviceProvider = services.BuildServiceProvider();
    }

    protected IMessageSenders MessageSenders => serviceProvider.GetRequiredService<IMessageSenders>();

    [TearDown]
    public void TearDown()
    {
        serviceProvider.Dispose();
    }

    protected T Resolve<T>()
        where T : notnull => serviceProvider.GetRequiredService<T>();
}
```

There are a few things to note here. We are creating a new service collection and create the service provider in the class's constructor. There is also a call to `AddApplicationServices`, which is a method we haven't seen before. This method comes from [ServiceCollectionExtensions.cs](Conqueror.Recipes.Messaging.TestingHandlers/ServiceCollectionExtensions.cs) and registers all services contained in its project. This is a recommended practice for modular system design.

The class uses the `[TearDown]` attribute to dispose the service provider after each test. For some testing frameworks there is another aspect you need to be careful of: they might re-use the same class instance for multiple tests. This could cause undesired side-effects, and therefore we recommend to configure your test framework to create a new class instance for each test. Since we are using [NUnit](https://nunit.org) in this recipe, we can do this with an assembly attribute in a new file `AssemblyAttributes.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingHandlers.Tests/AssemblyAttributes.cs)).

```cs
[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
```

Now we can use this test base for our `IncrementCounter` handler tests. Create a new test class called `IncrementCounterTests.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingHandlers.Tests/IncrementCounterTests.cs)) with some helper constants and properties:

```cs
namespace Conqueror.Recipes.Messaging.TestingHandlers.Tests;

[TestFixture]
public class IncrementCounterTests : TestBase
{
    private const string TestCounterName = "test-counter";

    private IncrementCounter.IHandler Handler => MessageSenders.For(IncrementCounter.T);

    private CountersRepository CountersRepository => Resolve<CountersRepository>();
}
```

The `Handler` and `CountersRepository` properties allow accessing the handler and repository conveniently in each test.

Our first test is going to validate what happens when we increment a non-existing counter:

```cs
[Test]
public async Task GivenNonExistingCounter_WhenIncrementingCounter_CounterIsCreatedAndInitialValueIsReturned()
{
    var response = await Handler.Handle(new(TestCounterName));

    var storedCounterValue = await CountersRepository.GetCounterValue(TestCounterName);

    Assert.That(storedCounterValue, Is.EqualTo(1).And.EqualTo(response.NewCounterValue));
}
```

As you can see, the test is short and clear, thanks to our test infrastructure.

> We validate the result by checking the repository directly, which is a small violation of the black-box testing approach. The alternative would be to fetch the counter's value with the `GetCounterValue` message, but this adds a dependency to this message to the tests for `IncrementCounter`. A third option would be to not test the messages separately but instead test the whole counter "domain" together by creating more end-to-end test cases like `GivenNonExistingCounter_WhenIncrementingCounter_CounterIsCreatedAndValueCanBeFetched`. Which of these approaches you choose is up to you to decide, they all have different trade-offs.

Let's add another test for incrementing an existing counter:

```cs
[Test]
public async Task GivenExistingCounter_WhenIncrementingCounter_CounterIsIncrementedAndValueIsReturned()
{
    await CountersRepository.SetCounterValue(TestCounterName, 10);

    var response = await Handler.Handle(new(TestCounterName));

    var storedCounterValue = await CountersRepository.GetCounterValue(TestCounterName);

    Assert.That(storedCounterValue, Is.EqualTo(11).And.EqualTo(response.NewCounterValue));
}
```

Another short and clear test.

Finally we need to test the side-effect. We want to verify that a notification is sent when a counter is incremented beyond the threshold of 1000. Unfortunately there is no easy way to verify this with the existing production code, so we are going to use a [mock object](https://en.wikipedia.org/wiki/Mock_object). We already added the [NSubstitute](https://nsubstitute.github.io) package when we created the test project, so let's add this as a global using statement to `Usings.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingHandlers.Tests/Usings.cs)) so that we don't need to repeatedly add usings for `NSubstitute`:

```diff
global using NUnit.Framework;
+ global using NSubstitute;
```

Since we resolve the handler from the service provider for testing, we need to register the mock object in the service provider. Apply the following changes to `TestBase.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingHandlers.Tests/TestBase.cs)) to replace `IAdminNotificationService` with a mock object:

```diff
  using Microsoft.Extensions.DependencyInjection;
+ using Microsoft.Extensions.DependencyInjection.Extensions;

  namespace Conqueror.Recipes.Messaging.TestingHandlers.Tests;

  public abstract class TestBase
  {
      private readonly ServiceProvider serviceProvider;

      protected TestBase()
      {
          var services = new ServiceCollection();

          services.AddApplicationServices();
+
+         services.Replace(ServiceDescriptor.Singleton(AdminNotificationServiceMock));

          serviceProvider = services.BuildServiceProvider();
      }

      protected IMessageSenders MessageSenders => serviceProvider.GetRequiredService<IMessageSenders>();
+
+     protected IAdminNotificationService AdminNotificationServiceMock { get; } =
+         Substitute.For<IAdminNotificationService>();

      [TearDown]
      public void TearDown()
      {
          serviceProvider.Dispose();
      }
  }
```

> As with the database topic mentioned at the beginning of this recipe, if possible our recommendation is to not mock dependencies at a low level but instead try to use a real instance of the dependency and then assert the results on that dependency. For example, if the admin notification service would be connected via a message queue, then it would be a good idea to spin up a real message queue for the tests and assert that a real message was published. However, sometimes you may want to use a more low-level mocking approach like we have shown here, which can speed up tests and make them more reliable.

With this change we can now write a test to verify that the notification is sent:

```cs
[Test]
public async Task GivenExistingCounter_WhenIncrementingCounterAboveThreshold_AdminNotificationIsSent()
{
    await CountersRepository.SetCounterValue(TestCounterName, 999);

    _ = await Handler.Handle(new(TestCounterName));

    await AdminNotificationServiceMock.Received(1)
        .SendCounterIncrementedBeyondThresholdNotification(TestCounterName);
}
```

Still short and clear thanks to our `TestBase`.

For completeness, let's also add a test for the negative case, i.e. that no notification is sent as long as a counter is incremented below the threshold.

```cs
[Test]
public async Task GivenExistingCounter_WhenIncrementingCounterBelowThreshold_NoAdminNotificationIsSent()
{
    await CountersRepository.SetCounterValue(TestCounterName, 10);

    _ = await Handler.Handle(new(TestCounterName));

    await AdminNotificationServiceMock.DidNotReceive()
        .SendCounterIncrementedBeyondThresholdNotification(TestCounterName);
}
```

And that concludes this recipe for testing message handlers with **Conqueror**. In summary, we recommend the following when testing handlers:

- always test handlers through `IMessageSenders.For(MessageType.T)` to get the handler interface
- focus on testing the public API (i.e. messages) of your application instead of testing implementation details
- consolidate common setup logic into a base class
- minimize the amount of mocking of external dependencies like databases, but if you need to mock them, create the mocks centrally in a test base class

As the next step you can explore other messaging recipes.

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.testing-handlers]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
