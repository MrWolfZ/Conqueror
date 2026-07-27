# Conqueror recipe (Signalling): testing handlers

This recipe shows how simple it is to test your signal handlers with **Conqueror**.

The handlers we are going to test are similar to those we built in the recipe for [getting started](../getting-started#readme). If you have not yet read that recipe, we recommend you take a look before you start with this one.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/signalling/testing-handlers) and open the solution file in your IDE (note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/signalling/testing-handlers).

The application we will be testing reacts to a counter being incremented. When you use **Conqueror** signalling, your signals represent an event that happened, and any number of handlers can react to it independently. In code, the signal our handlers react to is represented with the following type:

```cs
[Signal]
public partial record CounterIncremented(string CounterName, int NewValue);
```

There are two handlers reacting to this signal. The first keeps running statistics about how many increments have been recorded, storing the running total in a singleton so that it survives across the transient handler instances:

```cs
internal partial class StatisticsHandler(CounterStatistics statistics) : CounterIncremented.IHandler
{
    public async Task Handle(CounterIncremented signal, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        statistics.RecordedIncrements++;
    }
}
```

The second sends a notification to an administrator whenever a counter is incremented beyond a threshold of 1000 (through the [IAdminNotificationService](Conqueror.Recipes.Signalling.TestingHandlers/IAdminNotificationService.cs)), which is admittedly not very realistic for such a simple application, but it serves well to illustrate how to deal with such side-effects during testing:

```cs
internal partial class NotificationHandler(IAdminNotificationService adminNotificationService)
    : CounterIncremented.IHandler
{
    public async Task Handle(CounterIncremented signal, CancellationToken cancellationToken = default)
    {
        if (signal.NewValue >= 1000)
        {
            await adminNotificationService.SendCounterIncrementedBeyondThresholdNotification(
                signal.CounterName
            );
        }
    }
}
```

Feel free to take a look at the full code for [StatisticsHandler](Conqueror.Recipes.Signalling.TestingHandlers/StatisticsHandler.cs) and [NotificationHandler](Conqueror.Recipes.Signalling.TestingHandlers/NotificationHandler.cs).

For testing applications written with **Conqueror**, we recommend the [black-box testing](https://en.wikipedia.org/wiki/Black-box_testing) approach. With this approach you only test the observable behavior of your application without any knowledge about its internal implementation. For signalling this means we do not instantiate handler classes directly, but instead we **publish the signal** through its interface, exactly as production code does, and then assert on the observable effects (a service's state, or a call to a dependency). Publishing a signal reaches all registered handlers, so a test can register just the handler under test to keep it isolated.

> In a real application, the statistics may be persisted in some kind of database. We still recommend to let the tests use the real store (and therefore real database) to make them test the application behavior as closely to production as possible. You can use techniques like [test containers](https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/) to achieve that.

Given that we want to test the observable behavior, it follows naturally that we should not be testing signal handler classes directly, but instead we should publish the signal through the publisher interface. This means we need to set up the handlers and their dependencies inside the tests. We'll look at two different ways for doing this.

> We're using the [NUnit](https://nunit.org) framework in this recipe, but any of the points discussed here apply to any other testing framework as well.

Let's start by creating a new test project and adding the dependencies (if you prefer you can of course create the project via your IDE).

```sh
dotnet new nunit -n Conqueror.Recipes.Signalling.TestingHandlers.Tests

# add a reference to the implementation project
dotnet add Conqueror.Recipes.Signalling.TestingHandlers.Tests reference Conqueror.Recipes.Signalling.TestingHandlers

# add the NSubstitute package for creating mock objects
dotnet add Conqueror.Recipes.Signalling.TestingHandlers.Tests package NSubstitute

# add the new project to the solution
dotnet sln Conqueror.Recipes.Signalling.TestingHandlers.sln add Conqueror.Recipes.Signalling.TestingHandlers.Tests
```

Now we can start testing the [StatisticsHandler](Conqueror.Recipes.Signalling.TestingHandlers/StatisticsHandler.cs) by creating a new test class called `StatisticsHandlerTests.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.TestingHandlers.Tests/StatisticsHandlerTests.cs)):

```cs
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.Signalling.TestingHandlers.Tests;

[TestFixture]
public class StatisticsHandlerTests
{
}
```

To test the signal handler we need to publish the signal. To do this we'll create a service collection manually, register only the handler under test and its dependencies, and then publish the signal through the resolved publisher. Registering only the `StatisticsHandler` keeps the test isolated: no other handler reacts to the signal, so the observed effect can only come from the handler we are testing. Let's do this in our first test, which is going to verify that publishing the signal records an increment. For naming the tests, we recommend the [given-when-then](https://martinfowler.com/bliki/GivenWhenThen.html) style.

```cs
[Test]
public async Task GivenNoRecordedIncrements_WhenCounterIncrementedIsPublished_IncrementIsRecorded()
{
    var services = new ServiceCollection();

    services.AddSignalHandler<StatisticsHandler>()
            .AddSingleton<CounterStatistics>();

    await using var serviceProvider = services.BuildServiceProvider();

    var publisher = serviceProvider.GetRequiredService<ISignalPublishers>().For(CounterIncremented.T);

    await publisher.Handle(new("test-counter", 1));

    var statistics = serviceProvider.GetRequiredService<CounterStatistics>();

    Assert.That(statistics.RecordedIncrements, Is.EqualTo(1));
}
```

If you are following along with coding, you can now run this test, and it should succeed.

Let's add another test for publishing the signal when increments have already been recorded.

```cs
[Test]
public async Task GivenRecordedIncrements_WhenCounterIncrementedIsPublished_IncrementIsRecorded()
{
    var services = new ServiceCollection();

    services.AddSignalHandler<StatisticsHandler>()
            .AddSingleton<CounterStatistics>();

    await using var serviceProvider = services.BuildServiceProvider();

    var publisher = serviceProvider.GetRequiredService<ISignalPublishers>().For(CounterIncremented.T);
    var statistics = serviceProvider.GetRequiredService<CounterStatistics>();

    statistics.RecordedIncrements = 10;

    await publisher.Handle(new("test-counter", 1));

    Assert.That(statistics.RecordedIncrements, Is.EqualTo(11));
}
```

As you can see there are a few repetitions, so in accordance with the [DRY principle](https://en.wikipedia.org/wiki/Don't_repeat_yourself), we can extract a constant and method:

```diff
[TestFixture]
public class StatisticsHandlerTests
{
+   private const string TestCounterName = "test-counter";
+
    [Test]
    public async Task GivenNoRecordedIncrements_WhenCounterIncrementedIsPublished_IncrementIsRecorded()
    {
-       var services = new ServiceCollection();
-
-       services.AddSignalHandler<StatisticsHandler>()
-               .AddSingleton<CounterStatistics>();
-
-       await using var serviceProvider = services.BuildServiceProvider();
+       await using var serviceProvider = BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<ISignalPublishers>().For(CounterIncremented.T);

-       await publisher.Handle(new("test-counter", 1));
+       await publisher.Handle(new(TestCounterName, 1));

        var statistics = serviceProvider.GetRequiredService<CounterStatistics>();

        Assert.That(statistics.RecordedIncrements, Is.EqualTo(1));
    }

    [Test]
    public async Task GivenRecordedIncrements_WhenCounterIncrementedIsPublished_IncrementIsRecorded()
    {
-       var services = new ServiceCollection();
-
-       services.AddSignalHandler<StatisticsHandler>()
-               .AddSingleton<CounterStatistics>();
-
-       await using var serviceProvider = services.BuildServiceProvider();
+       await using var serviceProvider = BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<ISignalPublishers>().For(CounterIncremented.T);
        var statistics = serviceProvider.GetRequiredService<CounterStatistics>();

        statistics.RecordedIncrements = 10;

-       await publisher.Handle(new("test-counter", 1));
+       await publisher.Handle(new(TestCounterName, 1));

        Assert.That(statistics.RecordedIncrements, Is.EqualTo(11));
    }
+
+   private static ServiceProvider BuildServiceProvider()
+   {
+       return new ServiceCollection().AddSignalHandler<StatisticsHandler>()
+                                     .AddSingleton<CounterStatistics>()
+                                     .BuildServiceProvider();
+   }
}
```

This concludes the tests for the [StatisticsHandler](Conqueror.Recipes.Signalling.TestingHandlers/StatisticsHandler.cs). We used the approach of creating a service provider with the minimal services required for our tests. This works well, but adds a little bit of noise to the tests. Next, we are going to write tests for the [NotificationHandler](Conqueror.Recipes.Signalling.TestingHandlers/NotificationHandler.cs) using a slightly different approach.

The [NotificationHandler](Conqueror.Recipes.Signalling.TestingHandlers/NotificationHandler.cs) contains something which is quite common: a side-effect. Side-effects are things that happen as the result of publishing a signal, but are not directly visible in any state we own. In this case the side-effect is sending a notification to an administrator whenever a counter is incremented beyond a threshold of 1000 (through the [IAdminNotificationService](Conqueror.Recipes.Signalling.TestingHandlers/IAdminNotificationService.cs)).

In contrast to testing our `StatisticsHandler` above, we're going to consolidate the test setup into a dedicated _test host_ class which each test creates and disposes. For test infrastructure like this we prefer composition over inheritance, since it keeps each test explicit and self-contained, and avoids the fragility that base classes tend to introduce. Let's create a new file `TestHost.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.TestingHandlers.Tests/TestHost.cs)) and add the following content:

```cs
namespace Conqueror.Recipes.Signalling.TestingHandlers.Tests;

using Microsoft.Extensions.DependencyInjection;

internal sealed class TestHost : IAsyncDisposable
{
    private readonly ServiceProvider serviceProvider;

    private TestHost()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        serviceProvider = services.BuildServiceProvider();
    }

    public ISignalPublishers SignalPublishers => serviceProvider.GetRequiredService<ISignalPublishers>();

    public static TestHost Create() => new();

    public ValueTask DisposeAsync() => serviceProvider.DisposeAsync();

    public T Resolve<T>()
        where T : notnull => serviceProvider.GetRequiredService<T>();
}
```

There are a few things to note here. The constructor is private, and instances are created through the static `Create` factory method. The class implements `IAsyncDisposable`, so each test can create its own host with `await using` and have the service provider disposed automatically at the end of the test. There is also a call to `AddApplicationServices`, which is a method we haven't seen before. This method comes from [ServiceCollectionExtensions.cs](Conqueror.Recipes.Signalling.TestingHandlers/ServiceCollectionExtensions.cs) and registers all services contained in its project, including all its signal handlers. This is a recommended practice for modular system design.

> Because `AddApplicationServices` registers every handler, publishing a signal through this host reaches *all* handlers, not just the one we are testing. That is fine here since we only assert on the effect of the `NotificationHandler`, but it is the reason the isolated single-handler registration we used above is worth keeping in your toolbox.

Now we can use this test host for our `NotificationHandler` tests. Create a new test class called `NotificationHandlerTests.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.TestingHandlers.Tests/NotificationHandlerTests.cs)) with a helper constant:

```cs
namespace Conqueror.Recipes.Signalling.TestingHandlers.Tests;

[TestFixture]
public class NotificationHandlerTests
{
    private const string TestCounterName = "test-counter";
}
```

We want to verify that a notification is sent when a counter is incremented beyond the threshold of 1000. Unfortunately there is no easy way to verify this with the existing production code, so we are going to use a [mock object](https://en.wikipedia.org/wiki/Mock_object). We already added the [NSubstitute](https://nsubstitute.github.io) package when we created the test project, so let's add this as a global using statement to `Usings.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.TestingHandlers.Tests/Usings.cs)) so that we don't need to repeatedly add usings for `NSubstitute`:

```diff
global using NUnit.Framework;
+ global using NSubstitute;
```

Since we publish the signal to the handler resolved from the service provider, we need to register the mock object in the service provider. Apply the following changes to `TestHost.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.TestingHandlers.Tests/TestHost.cs)) to replace `IAdminNotificationService` with a mock object:

```diff
  namespace Conqueror.Recipes.Signalling.TestingHandlers.Tests;

  using Microsoft.Extensions.DependencyInjection;
+ using Microsoft.Extensions.DependencyInjection.Extensions;

  internal sealed class TestHost : IAsyncDisposable
  {
      private readonly ServiceProvider serviceProvider;

      private TestHost()
      {
          var services = new ServiceCollection();

          services.AddApplicationServices();
+
+         services.Replace(ServiceDescriptor.Singleton(AdminNotificationServiceMock));

          serviceProvider = services.BuildServiceProvider();
      }

      public ISignalPublishers SignalPublishers => serviceProvider.GetRequiredService<ISignalPublishers>();
+
+     public IAdminNotificationService AdminNotificationServiceMock { get; } =
+         Substitute.For<IAdminNotificationService>();

      public static TestHost Create() => new();

      public ValueTask DisposeAsync() => serviceProvider.DisposeAsync();
  }
```

> As with the database topic mentioned at the beginning of this recipe, if possible our recommendation is to not mock dependencies at a low level but instead try to use a real instance of the dependency and then assert the results on that dependency. For example, if the admin notification service would be connected via a message queue, then it would be a good idea to spin up a real message queue for the tests and assert that a real message was published. However, sometimes you may want to use a more low-level mocking approach like we have shown here, which can speed up tests and make them more reliable.

With this change we can now write a test to verify that the notification is sent:

```cs
[Test]
public async Task GivenCounterIncrementedBeyondThreshold_WhenSignalIsPublished_AdminNotificationIsSent()
{
    await using var host = TestHost.Create();

    var publisher = host.SignalPublishers.For(CounterIncremented.T);

    await publisher.Handle(new(TestCounterName, 1000));

    await host.AdminNotificationServiceMock.Received(1)
        .SendCounterIncrementedBeyondThresholdNotification(TestCounterName);
}
```

Short and clear thanks to our `TestHost`.

For completeness, let's also add a test for the negative case, i.e. that no notification is sent as long as a counter is incremented below the threshold.

```cs
[Test]
public async Task GivenCounterIncrementedBelowThreshold_WhenSignalIsPublished_NoAdminNotificationIsSent()
{
    await using var host = TestHost.Create();

    var publisher = host.SignalPublishers.For(CounterIncremented.T);

    await publisher.Handle(new(TestCounterName, 10));

    await host.AdminNotificationServiceMock.DidNotReceive()
        .SendCounterIncrementedBeyondThresholdNotification(TestCounterName);
}
```

And that concludes this recipe for testing signal handlers with **Conqueror**. In summary, we recommend the following when testing handlers:

- always test handlers by publishing the signal through `ISignalPublishers.For(SignalType.T)` instead of instantiating handler classes directly
- focus on testing the observable behavior of your application (state changes and calls to dependencies) instead of testing implementation details
- register only the handler under test when you want to test a single handler in isolation
- consolidate common setup logic into a test host class that each test creates and disposes via `await using`
- minimize the amount of mocking of external dependencies like databases, but if you need to mock them, create the mocks centrally in the test host

As the next step you can explore other signalling recipes.

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.signalling.testing-handlers]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
