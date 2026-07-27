# Conqueror recipe (Messaging): moving from a modular monolith to a distributed system

This recipe shows how to transform a modular monolith built with **Conqueror** into a distributed application.

In this recipe we will transform the monolith we built in the recipe for [creating a clean architecture](../clean-architecture#readme). If you have not yet read that recipe, we recommend you take a look at it before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/messaging/monolith-to-distributed) and open the solution file in your IDE (note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `.completed` folder of the recipe's solution.

The application, which we will transform into a distributed system, is managing a set of named counters which can be incremented by users. The application also tracks a history for which counter was incremented by which user and allows fetching the most recently incremented counter for a given user. The application has two bounded contexts, `Counters` and `UserHistory`. Their APIs can be expressed in code as follows:

```cs
// Counters API

[HttpMessage<IncrementCounterResponse>(HttpMethod = "POST")]
public partial record IncrementCounter(string CounterName, string UserId);

public record IncrementCounterResponse(int NewCounterValue);

[HttpMessage<GetCounterValueResponse>(HttpMethod = "GET")]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);

// UserHistory API

// note that this is not an HTTP message (yet), since it is called from the `Counters` bounded context in-memory
[Message]
public partial record SetMostRecentlyIncrementedCounterForUser(string UserId, string CounterName);

[HttpMessage<GetMostRecentlyIncrementedCounterForUserResponse>(HttpMethod = "GET")]
public partial record GetMostRecentlyIncrementedCounterForUser(string UserId);

public record GetMostRecentlyIncrementedCounterForUserResponse(string? CounterName);
```

> During the transformation, we assume that the distributed system will stay in the same code repository that the monolith is part of, essentially making the repository a [monorepo](https://en.wikipedia.org/wiki/Monorepo). Depending on your project setup, you may want to extract each bounded context into a separate repository. This recipe contains hints for how to do this at the end, after the transformation is done.

Before we start with the transformation, let's briefly discuss the trade-offs involved in such a change. Distributed systems offer better horizontal scalability, deployment independence, smaller deployable units, faster build pipelines, and other benefits. However, distributed systems are also more complex, harder to operate, need more resilience to deal with communication failures, etc. Make sure that you and your team have properly considered these trade-offs before deciding on which approach you want to use.

> One of the big benefits of using **Conqueror** is, that it is easy to start with a modular monolith and transform it into a distributed system with minimal effort (as we will explore in the rest of this recipe). This allows you to delay the decision about building a distributed system to a point at which you have a better understanding of the business domain and the architectural drivers.

The first step in transforming our application is to consider the entry points. In the modular monolith we only have a single entry point for the web API. In a distributed system each bounded context will have its own web API entry point. For simplicity we have already created the `Counters.EntryPoint.WebApi` and `UserHistory.EntryPoint.WebApi` projects, but they are still missing their setup logic.

> The simplest way to create the new entry point projects is to copy the monolith's entry point project folder, rename the copied folder, rename the `.csproj` file, and remove the references to other bounded contexts from the `.csproj` file.

Let's start by refactoring the `UserHistory` context, since it is the simpler one of the two. Replace the content of `UserHistoryProgram.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.EntryPoint.WebApi/UserHistoryProgram.cs)) with the following:

```cs
namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.EntryPoint.WebApi;

using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Application;
using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Infrastructure;

// use a class with a custom name instead of a top-level program to distinguish
// this entry point from the ones of other bounded contexts
public sealed class UserHistoryProgram
{
    private UserHistoryProgram()
    {
    }

    public static async Task Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services
               .AddConquerorHttpServerAspNetCore()
               .AddSwaggerGen();

        builder.Services
               .AddUserHistoryApplication()
               .AddUserHistoryInfrastructure();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapMessageEndpoints();

        await app.RunAsync();
    }
}
```

The file content is almost identical to the entry point of the monolith. The main differences are that it wraps the setup code in a class called `UserHistoryProgram` (which helps distinguish it from other entry points; the private constructor just prevents the class from being instantiated) and that it does not add services from the `Counters` context.

Thanks to our clean application architecture, this single change is already enough to turn the `UserHistory` context into a standalone web application. You can run the app and it will serve its messages via HTTP. However, there are a few more changes we need to do. Firstly, the `SetMostRecentlyIncrementedCounterForUser` message will need to be called by the `Counters` context via HTTP. Therefore, let's turn it into an HTTP message ([view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Contracts/SetMostRecentlyIncrementedCounterForUser.cs)):

```diff
- // this message has no HTTP attribute: it is an internal contract used by other bounded contexts
- // (e.g. Counters) to notify the UserHistory context, not something we expose to the outside world
- [Message]
+ // this message is exposed via HTTP so that other services (e.g. the Counters web app)
+ // can call it across the service boundary
+ [HttpMessage(HttpMethod = "POST")]
  public partial record SetMostRecentlyIncrementedCounterForUser(string UserId, string CounterName);
```

The other aspect we need to adjust are the tests. In the `UserHistory.Tests.csproj` file, replace the reference to the monolith's entry point with a reference to the `UserHistory.EntryPoint.WebApi` project. Afterwards, we need to make the following changes in `TestHost.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Tests/TestHost.cs)):

```diff
  namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Tests;

+ using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.EntryPoint.WebApi;
+
  internal sealed class TestHost : IAsyncDisposable
  {
-     // bootstrap the whole application so that the tests exercise each context with
-     // full integration with the other contexts, just like in production
-     private readonly WebApplicationFactory<Program> applicationFactory = new();
+     // bootstrap the UserHistory web application so that the tests exercise it
+     // just like in production
+     private readonly WebApplicationFactory<UserHistoryProgram> applicationFactory = new();
```

With these changes, the tests run against the `UserHistory` web app instead of the monolith's web app. The final change is to run the test for the `SetMostRecentlyIncrementedCounterForUser` message through the HTTP API instead of invoking it in-process, since via HTTP is how the message is going to be called in production. Sending messages via HTTP requires the client portion of **Conqueror**'s HTTP transport, so add a reference to the [Conqueror.Transport.Http.Client](https://www.nuget.org/packages/Conqueror.Transport.Http.Client) package to the test project:

```sh
dotnet add Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Tests package Conqueror.Transport.Http.Client
```

Then add the following members to the `TestHost` (and dispose the new service provider in `DisposeAsync`; [view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Tests/TestHost.cs)):

```cs
// dedicated service provider for sending messages to the web application from the
// outside, to prevent interference with the services of the server application
private readonly ServiceProvider clientServices = new ServiceCollection().AddConquerorHttpClient().BuildServiceProvider();

// senders resolved from here send messages through the web app's HTTP API, just like
// a remote caller (e.g. the Counters web app) would in production
public IMessageSenders HttpMessageSenders => clientServices.GetRequiredService<IMessageSenders>();

public HttpClient HttpClient { get; }
```

The `HttpClient` property is initialized in the constructor with `applicationFactory.CreateClient()`, which creates an HTTP client that talks to the in-memory test server. With this, we can change the test in `SetMostRecentlyIncrementedCounterForUserTests.cs` to send the message via HTTP ([view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Tests/SetMostRecentlyIncrementedCounterForUserTests.cs)):

```diff
  await using var host = TestHost.Create();

- await host.MessageSenders.For(SetMostRecentlyIncrementedCounterForUser.T).Handle(new(TestUserId, TestCounterName));
+ // send the message through the web app's HTTP API, just like the Counters
+ // web app does in production
+ await host.HttpMessageSenders.For(SetMostRecentlyIncrementedCounterForUser.T)
+           .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(host.HttpClient))
+           .Handle(new(TestUserId, TestCounterName));
```

> The address passed to `UseHttp` is ignored when the supplied `HttpClient` has a `BaseAddress`, which is the case for clients created by a `WebApplicationFactory`.

This completes the transformation of the `UserHistory` context. Now we can turn our attention to the `Counters` context. Replace the content of `CountersProgram.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.EntryPoint.WebApi/CountersProgram.cs)) with the following:

```cs
namespace Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.EntryPoint.WebApi;

using Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application;
using Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Infrastructure;

// use a class with a custom name instead of a top-level program to distinguish
// this entry point from the ones of other bounded contexts
public sealed class CountersProgram
{
    private CountersProgram()
    {
    }

    public static async Task Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services
               .AddConquerorHttpServerAspNetCore()
               .AddSwaggerGen();

        builder.Services
               .AddCountersApplication()
               .AddCountersInfrastructure();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapMessageEndpoints();

        await app.RunAsync();
    }
}
```

There is one more design decision to make: the `IncrementCounterHandler` sends the `SetMostRecentlyIncrementedCounterForUser` message through the injected `IMessageSenders`, which dispatches it to the handler registered in the same process. In our distributed system, the message needs to be sent via HTTP instead. We could configure this at the call site in the handler with `.WithTransport(b => b.UseHttp(...))`, but that would couple our application layer to HTTP and to the configuration for the `UserHistory` web app's address. Instead, we use the [dependency inversion principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle) once more: the handler declares a dependency on the generated `SetMostRecentlyIncrementedCounterForUser.IHandler` interface, and the entry point decides how the message is sent. Make the following changes in `IncrementCounterHandler.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application/IncrementCounterHandler.cs)):

```diff
  internal partial class IncrementCounterHandler(
      ICountersReadRepository countersReadRepository,
      ICountersWriteRepository countersWriteRepository,
-     IMessageSenders senders)
+     SetMostRecentlyIncrementedCounterForUser.IHandler setMostRecentlyIncrementedCounterForUserHandler)
      : IncrementCounter.IHandler
```

```diff
-         // integrate with the UserHistory context through its contract, without a direct dependency on its implementation
-         await senders.For(SetMostRecentlyIncrementedCounterForUser.T)
-                      .Handle(new SetMostRecentlyIncrementedCounterForUser(message.UserId, message.CounterName), cancellationToken);
+         // integrate with the UserHistory context through its contract; the transport over
+         // which the message is sent is configured in the entry point's composition root
+         await setMostRecentlyIncrementedCounterForUserHandler
+             .Handle(new SetMostRecentlyIncrementedCounterForUser(message.UserId, message.CounterName), cancellationToken);
```

> With this change the monolith's entry point becomes obsolete (it does not register anything for the new dependency), so you can delete the `EntryPoint.WebApi` project.

The app compiles successfully, but when you run it, it will fail the startup with an error like this:

```log
Unhandled exception. System.AggregateException: Some services are not able to be constructed (Error while validating the service descriptor 'ServiceType: Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application.IncrementCounterHandler Lifetime: Transient ImplementationType: Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application.IncrementCounterHandler': Unable to resolve service for type 'Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Contracts.SetMostRecentlyIncrementedCounterForUser+IHandler' while attempting to activate 'Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application.IncrementCounterHandler'.)
```

This is because nothing registers a sender for the `SetMostRecentlyIncrementedCounterForUser` message yet. We need to make a few changes, to get this to work. First, we need to add a package reference to the [Conqueror.Transport.Http.Client](https://www.nuget.org/packages/Conqueror.Transport.Http.Client) package in our entry point project:

```sh
dotnet add Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.EntryPoint.WebApi package Conqueror.Transport.Http.Client
```

Next, in `appsettings.json` ([view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.EntryPoint.WebApi/appsettings.json)), add a config entry for the `UserHistory` context's app:

```diff
  {
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
+   "UserHistoryBaseAddress": "http://localhost:5002",
    "AllowedHosts": "*"
  }
```

Then, register a ready-configured sender for the message in `CountersProgram.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.EntryPoint.WebApi/CountersProgram.cs)):

```cs
// configure the sender for the UserHistory context's message to use the HTTP
// transport with the base address of the UserHistory web application
builder.Services
       .AddConquerorHttpClient()
       .AddSingleton<SetMostRecentlyIncrementedCounterForUser.IHandler>(
           p => p.GetRequiredService<IMessageSenders>()
                 .For(SetMostRecentlyIncrementedCounterForUser.T)
                 .WithPipeline(pipeline => pipeline.UseDefault())
                 .WithTransport(b => b.UseHttp(p.GetRequiredService<IConfiguration>().GetValue<Uri>("UserHistoryBaseAddress")!)));
```

`AddConquerorHttpClient` registers the services which the HTTP transport requires for sending messages. The `AddSingleton` registration creates a sender which implements the generated `SetMostRecentlyIncrementedCounterForUser.IHandler` interface, is configured to use the `UserHistory` context's base address from the configuration, and runs our default pipeline, so that the outgoing message is logged in the `Counters` app as well.

With these changes in place, you can run both the `UserHistory` app and the `Counters` app, and invoke the `IncrementCounter` message as well as the `GetMostRecentlyIncrementedCounterForUser` message:

```sh
curl http://localhost:5001/api/incrementCounter --data '{"counterName":"test","userId":"user1"}' -H 'Content-Type: application/json'
# prints {"newCounterValue":1}

curl 'http://localhost:5002/api/getMostRecentlyIncrementedCounterForUser?userId=user1'
# prints {"counterName":"test"}
```

If you look at the logs of the applications, you will see output like the following.

`Counters` app:

```log
info: Counters.Contracts.IncrementCounter[711195907]
      Handling http message of type 'IncrementCounter' with payload {"CounterName":"test","UserId":"user1"} (Message ID: fda87412e007a23f, Trace ID: 20cc508f49b9f7b50e5b88ba3988a763)
info: UserHistory.Contracts.SetMostRecentlyIncrementedCounterForUser[711195907]
      Sending http message of type 'SetMostRecentlyIncrementedCounterForUser' with payload {"UserId":"user1","CounterName":"test"} (Message ID: 9d371a465a08b929, Trace ID: 20cc508f49b9f7b50e5b88ba3988a763)
info: UserHistory.Contracts.SetMostRecentlyIncrementedCounterForUser[412531951]
      Sent http message of type 'SetMostRecentlyIncrementedCounterForUser' in 84.6194ms (Message ID: 9d371a465a08b929, Trace ID: 20cc508f49b9f7b50e5b88ba3988a763)
info: Counters.Contracts.IncrementCounter[412531951]
      Handled http message of type 'IncrementCounter' and got response {"NewCounterValue":1} in 105.4722ms (Message ID: fda87412e007a23f, Trace ID: 20cc508f49b9f7b50e5b88ba3988a763)
```

`UserHistory` app:

```log
info: UserHistory.Contracts.SetMostRecentlyIncrementedCounterForUser[711195907]
      Handling http message of type 'SetMostRecentlyIncrementedCounterForUser' with payload {"UserId":"user1","CounterName":"test"} (Message ID: 9d371a465a08b929, Trace ID: 20cc508f49b9f7b50e5b88ba3988a763)
info: UserHistory.Contracts.SetMostRecentlyIncrementedCounterForUser[412531951]
      Handled http message of type 'SetMostRecentlyIncrementedCounterForUser' in 25.5144ms (Message ID: 9d371a465a08b929, Trace ID: 20cc508f49b9f7b50e5b88ba3988a763)
info: UserHistory.Contracts.GetMostRecentlyIncrementedCounterForUser[711195907]
      Handling http message of type 'GetMostRecentlyIncrementedCounterForUser' with payload {"UserId":"user1"} (Message ID: 823d945958aee2c4, Trace ID: 6243b3cad8e1ca5cfd73e359ac7e17a5)
info: UserHistory.Contracts.GetMostRecentlyIncrementedCounterForUser[412531951]
      Handled http message of type 'GetMostRecentlyIncrementedCounterForUser' and got response {"CounterName":"test"} in 2.0818ms (Message ID: 823d945958aee2c4, Trace ID: 6243b3cad8e1ca5cfd73e359ac7e17a5)
```

For the execution of the `SetMostRecentlyIncrementedCounterForUser` message you can see the logs on both the client (i.e. `Counters` app, which logs `Sending`/`Sent`) and the server (i.e. `UserHistory` app, which logs `Handling`/`Handled`). Note that the `IncrementCounter` and `SetMostRecentlyIncrementedCounterForUser` messages have the same trace ID `20cc508f49b9f7b50e5b88ba3988a763`, which allows you to correlate log entries across the two applications (each message also has its own message ID to correlate log entries for that single message invocation, and this ID is also propagated across the HTTP call).

The final step in the transformation is to adjust the tests for the `Counters` context. In the monolith, the tests for the `IncrementCounter` message were using the real `UserHistory` context's message handlers. In a distributed system, you have two choices. Firstly, you could mock all messages from other contexts during testing. This is the simplest solution, but it means that you are not testing the integration of the bounded contexts, which may lead to bugs that only show themselves in deployed test or staging environments. The other option is to run the tests against both web apps, i.e. bootstrap both the `Counters` web API and the `UserHistory` web API during test setup, and configure the tests to use both apps. This is the option we are going to explore in this recipe.

First, in the `Counters.Tests.csproj` file, replace the reference to the monolith's entry point with references to both the `Counters.EntryPoint.WebApi` and `UserHistory.EntryPoint.WebApi` projects. Afterwards, replace the contents of `TestHost.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Tests/TestHost.cs)) with the following:

```cs
namespace Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Tests;

using Conqueror.Recipes.Messaging.MonolithToDistributed.Core.Application;
using Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.EntryPoint.WebApi;
using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Contracts;
using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.EntryPoint.WebApi;
using Microsoft.AspNetCore.Hosting;

internal sealed class TestHost : IAsyncDisposable
{
    // bootstrap both web applications so that the tests exercise the Counters context
    // with full integration with the UserHistory context, just like in production
    private readonly WebApplicationFactory<UserHistoryProgram> userHistoryApp = new();
    private readonly WebApplicationFactory<CountersProgram> countersAppFactory = new();
    private readonly WebApplicationFactory<CountersProgram> countersApp;
    private readonly HttpClient userHistoryHttpClient;

    private TestHost()
    {
        userHistoryHttpClient = userHistoryApp.CreateClient();

        countersApp = countersAppFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                // replace the sender for the UserHistory context's message so that it is
                // sent to the in-memory test server instead of the configured base address
                services.AddSingleton<SetMostRecentlyIncrementedCounterForUser.IHandler>(
                    p => p.GetRequiredService<IMessageSenders>()
                          .For(SetMostRecentlyIncrementedCounterForUser.T)
                          .WithPipeline(pipeline => pipeline.UseDefault())
                          .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(userHistoryHttpClient)))));
    }

    // messages are invoked in-process through the same public API the application uses
    public IMessageSenders MessageSenders => countersApp.Services.GetRequiredService<IMessageSenders>();

    // the UserHistory context's messages are invoked in-process on the UserHistory web app
    public IMessageSenders UserHistoryMessageSenders => userHistoryApp.Services.GetRequiredService<IMessageSenders>();

    public static TestHost Create() => new();

    public T ResolveOnServer<T>()
        where T : notnull => countersApp.Services.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await countersApp.DisposeAsync();
        await countersAppFactory.DisposeAsync();
        userHistoryHttpClient.Dispose();
        await userHistoryApp.DisposeAsync();
    }
}
```

This setup code bootstraps both web applications and overrides the registration for the `SetMostRecentlyIncrementedCounterForUser.IHandler` so that the message is sent to the `UserHistory` test server via HTTP. This is the payoff of registering a ready-configured sender in the entry point: the registration acts as a single seam for how the message is sent, which the tests can redirect without touching any application code.

The only adjustment in the tests themselves is in `IncrementCounterTests.cs`: the `GetMostRecentlyIncrementedCounterForUser` message is now handled by the `UserHistory` web app, so both calls that fetch it need to go through `UserHistoryMessageSenders` ([view completed file](.completed/Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Tests/IncrementCounterTests.cs)):

```diff
- var response1 = await host.MessageSenders.For(GetMostRecentlyIncrementedCounterForUser.T).Handle(new(TestUserId));
+ var response1 = await host.UserHistoryMessageSenders.For(GetMostRecentlyIncrementedCounterForUser.T).Handle(new(TestUserId));

  _ = await host.MessageSenders.For(IncrementCounter.T).Handle(new(counterName2, TestUserId));

- var response2 = await host.MessageSenders.For(GetMostRecentlyIncrementedCounterForUser.T).Handle(new(TestUserId));
+ var response2 = await host.UserHistoryMessageSenders.For(GetMostRecentlyIncrementedCounterForUser.T).Handle(new(TestUserId));
```

Except for this small change, the tests still work exactly as before. This is one of the awesome aspects of **Conqueror**, since it gives you the confidence that your system still works as it did in the monolith without requiring the effort of adjusting or rewriting tests.

Lastly, let's discuss how you could separate each bounded context into its own separate code repository (this approach is commonly called "polyrepo"). Performing such a separation gives each context's development team more flexibility and independence, but makes it harder to integrate the different web applications.

The first aspect you need to consider are your contracts. In the monorepo approach, each bounded context can reference other context's contracts with a direct code reference. In a polyrepo approach we recommend that you publish each context's contracts as a NuGet package, which can then be referenced by other contexts.

The second aspect to consider is testing. As we explored above, in a monorepo, we can use ASP.NET Core's test server to bootstrap multiple web applications during the test setup. In a polyrepo, it is more common to mock external dependencies. If you still want to test the integration of the different web applications, this can be done in multiple ways. For example, if your apps are packaged as [containers](https://en.wikipedia.org/wiki/Containerization_(computing)), you could start other apps as a container during testing, and then configure your message senders to talk to the containers. Alternatively, you could deploy your apps to a test or staging environment, and then configure your message senders to talk to the instances in that environment. Which of these approaches you want to choose depends on the complexity of your application, the size of your team, the maturity of your infrastructure, and other aspects that go beyond the scope of this recipe.

This completes the transformation of our example application into a distributed application. You can view the completed code [here](.completed) in your browser or in the `.completed` folder of the recipe's solution.

It is difficult to judge at what point you should consider transforming your monolithic application into a distributed application as discussed in this recipe. You need to consider drivers like horizontal scalability, deployment and development independence, CI pipeline performance, etc. For some projects it makes sense to build them distributed from the start. For other applications you may want to start with a monolith and then refactor your application over the course of its lifetime. Another option is to only extract those modules with specific requirements from the monolith into a separate application, and keep all other modules in the monolith. If and when you decide that you want to make your application distributed, **Conqueror** helps you by making the transformation as painless as possible, while giving you the flexibility to easily move back to a monolith.

In summary, if you and your team decide to build or transform your application as a distributed application, you need to do the following:

- create new entry points for each module or bounded context
- expose your messages via HTTP as necessary
- in apps with dependencies on other bounded contexts, register ready-configured HTTP senders for those contexts' messages
- adjust your tests to run against the individual web applications instead of the monolithic application

If you want to learn more about using **Conqueror**, head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.monolith-to-distributed]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
