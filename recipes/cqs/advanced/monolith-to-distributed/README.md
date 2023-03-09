# Conqueror recipe (CQS Advanced): moving from a modular monolith to a distributed system

This recipe shows how to transform a modular monolith built with  **Conqueror.CQS** into a distributed application.

In this recipe we will transform the monolith we built in the recipe for [creating a clean architecture](../clean-architecture#readme). If you have not yet read that recipe, we recommend you take a look at it before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/monolith-to-distributed) and open the solution file in your IDE (note that you need to have [.NET 6 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/monolith-to-distributed).

The application, which we will transform into a distributed system, is managing a set of named counters with can be incremented by users. The application also tracks a history for which counter was incremented by which user and allows fetching the most recently incremented counter for a given user. The application has two bounded contexts, `Counters` and `UserHistory`. Their APIs can be expressed in code as follows:

```cs
// Counters API

[HttpCommand]
public sealed record IncrementCounterCommand([Required] string CounterName, [Required] string UserId);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

[HttpQuery]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);

// UserHistory API

// note that this is not an HTTP command (yet), since it is called from the `Counters` bounded context in-memory
public sealed record SetMostRecentlyIncrementedCounterForUserCommand([Required] string UserId, [Required] string CounterName);

[HttpQuery]
public sealed record GetMostRecentlyIncrementedCounterForUserQuery([Required] string UserId);

public sealed record GetMostRecentlyIncrementedCounterForUserQueryResponse(string? CounterName);
```

> During the transformation, we assume that the distributed system will stay in the same code repository that the monolith is part of, essentially making the repository a [monorepo](https://en.wikipedia.org/wiki/Monorepo). Depending on your project setup, you may want to extract each bounded context into a separate repository. This recipe contains hints for how to do this at the end, after the transformation is done.

Before we start with the transformation, let's briefly discuss the trade-offs involved in such a change. Distributed systems offer better horizontal scalability, deployment independence, smaller deployable units, faster build pipelines, and other benefits. However, distributed systems are also more complex, harder to operate, need more resilience to deal with communication failures, etc. Make sure that you and your team have properly considered these trade-offs before deciding on which approach you want to use.

> One of the big benefits of using **Conqueror.CQS** is, that it is easy to start with a modular monolith and transform it into a distributed system with minimal effort (as we will explore in the rest of this recipe). This allows you to delay the decision about building a distributed system to a point at which you have a better understanding of the business domain and the architectural drivers.

The first step in transforming our application is to consider the entry points. In the modular monolith we only have a single entry point for the web API. In a distributed system each bounded context will have its own web API entry point. For simplicity we have already created the `Counters.EntryPoint.WebApi` and `UserHistory.EntryPoint.WebApi` projects, but they are still missing their setup logic.

> The simplest way to create the new entry point projects is to copy the monolith's entry point project folder, rename the copied folder, rename the `.csproj` file, and remove the references to other bounded contexts from the `.csproj` file.

Let's start by refactoring the `UserHistory` context, since it is the simpler one of the two. Replace the content of `UserHistoryProgram.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.EntryPoint.WebApi/UserHistoryProgram.cs)) with the following:

```cs
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Application;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Infrastructure;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.EntryPoint.WebApi;

// use a class with a custom name instead of a top-level program to distinguish
// this entry point from the ones of other bounded contexts
public sealed class UserHistoryProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
               .AddControllers()
               .AddConquerorCQSHttpControllers();

        builder.Services
               .AddEndpointsApiExplorer()
               .AddSwaggerGen();

        builder.Services
               .AddUserHistoryApplication()
               .AddUserHistoryInfrastructure();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapControllers();

        app.Run();
    }
}
```

The file content is almost identical to the entry point of the monolith. The only differences are that it wraps the setup code in a class called `UserHistoryProgram` (which helps distinguish it from other entry points) and that it does not add services from the `Counters` context.

Thanks to our clean application architecture, this single change is already enough to turn the `UserHistory` context into a standalone web application. You can run the app and it will serve its query via HTTP. However, there are a few more changes we need to do. Firstly, the `SetMostRecentlyIncrementedCounterForUserCommand` will need to be called by the `Counters` context via HTTP. Therefore, let's turn the command into an HTTP command:

```diff
+ [HttpCommand]
  public sealed record SetMostRecentlyIncrementedCounterForUserCommand([Required] string UserId, [Required] string CounterName);
```

The other aspect we need to adjust are the tests. In the `UserHistory.Tests.csproj` file, replace the reference to the monolith's entry point with a reference to the `UserHistory.EntryPoint.WebApi` project. Afterwards, we need to make the following changes in `TestBase.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Tests/TestBase.cs)]:

```diff
+ using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.EntryPoint.WebApi;
  using Microsoft.AspNetCore.Hosting;
  using Microsoft.Extensions.Logging;

  namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Tests;

  public abstract class TestBase : IDisposable
  {
-     private readonly WebApplicationFactory<Program> applicationFactory;
+     private readonly WebApplicationFactory<UserHistoryProgram> applicationFactory;
      private readonly ServiceProvider clientServices;
      private readonly HttpClient httpTestClient;

      protected TestBase()
      {
-         applicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
+         applicationFactory = new WebApplicationFactory<UserHistoryProgram>().WithWebHostBuilder(builder =>
          {
              // configure tests to produce logs; this is very useful to debug failing tests
              builder.ConfigureLogging(o => o.ClearProviders()

```

With these changes, the tests run against the `UserHistory` web app instead of the monolith's web app. The final change is to run the tests for the `SetMostRecentlyIncrementedCounterForUserCommand` against the HTTP API instead of running it directly on the server. This can be achieved by changing a single line of code in `SetMostRecentlyIncrementedCounterForUserCommandTests.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Tests/SetMostRecentlyIncrementedCounterForUserCommandTests.cs)]:

```diff
  private const string TestCounterName = "testCounter";
  private const string TestUserId = "testUser";

- private ISetMostRecentlyIncrementedCounterForUserCommandHandler CommandClient => ResolveOnServer<ISetMostRecentlyIncrementedCounterForUserCommandHandler>();
+ private ISetMostRecentlyIncrementedCounterForUserCommandHandler CommandClient => CreateCommandClient<ISetMostRecentlyIncrementedCounterForUserCommandHandler>();

  private IUserHistoryWriteRepository WriteRepository => ResolveOnServer<IUserHistoryWriteRepository>();
```

This completes the transformation of the `UserHistory` context. Now we can turn our attention to the `Counters` context. Replace the content of `CountersProgram.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.EntryPoint.WebApi/CountersProgram.cs)) with the following:

```cs
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Core.Application;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Application;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Infrastructure;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Contracts;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.EntryPoint.WebApi;

// use a class with a custom name instead of a top-level program to distinguish
// this entry point from the ones of other bounded contexts
public sealed class CountersProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
               .AddControllers()
               .AddConquerorCQSHttpControllers();

        builder.Services
               .AddEndpointsApiExplorer()
               .AddSwaggerGen();

        builder.Services
               .AddCountersApplication()
               .AddCountersInfrastructure();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapControllers();

        app.Run();
    }
}
```

The app compiles successfully, but when you run it, it will fail the startup with an error like this:

```log
Unable to resolve service for type 'Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Contracts.ISetMostRecentlyIncrementedCounterForUserCommandHandler' while attempting to activate 'Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Application.IncrementCounterCommandHandler'.
```

This is because the app doesn't know how to call the `SetMostRecentlyIncrementedCounterForUserCommand` via HTTP yet. We need to make a few changes, to get this to work. First, we need to add a package reference to [Conqueror.CQS.Transport.Http.Client](https://www.nuget.org/packages/Conqueror.CQS.Transport.Http.Client) package in our entry point project:

```sh
dotnet add Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.EntryPoint.WebApi package Conqueror.CQS.Transport.Http.Client
```

Next, in `appsettings.json` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.EntryPoint.WebApi/appsettings.json)), add a config entry for the `UserHistory` context's app:

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

Then, add an HTTP client for the command in `CountersProgram.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.EntryPoint.WebApi/CountersProgram.cs)):

```cs
builder.Services
       .AddConquerorCQSHttpClientServices()
       .AddConquerorCommandClient<ISetMostRecentlyIncrementedCounterForUserCommandHandler>(
           b => b.UseHttp(b.ServiceProvider.GetRequiredService<IConfiguration>().GetValue<Uri>("UserHistoryBaseAddress")),
           pipeline => pipeline.UseDefault());
```

The client is configured to use the `UserHistory` context's base address from the configuration, and we also configure the client's pipeline to use the same default pipeline that our handlers are using to add validation and logging (in a more complex application, you would likely want to create a dedicated default pipeline for HTTP command and query clients).

With these changes in place, you can run both the `UserHistory` app and the `Counters` app, and invoke the `IncrementCounterCommand` as well as the `GetMostRecentlyIncrementedCounterForUserQuery`:

```sh
curl http://localhost:5001/api/commands/incrementCounter --data '{"counterName":"test","userId":"user1"}' -H 'Content-Type: application/json'
# prints {"newCounterValue":1}

curl http://localhost:5002/api/queries/getMostRecentlyIncrementedCounterForUser?userId=user1
# prints {"counterName":"test"}
```

If you look at the logs of the applications, you will see output like the following.

`Counters` app:

```log
info: Counters.Contracts.IncrementCounterCommand[0]
      Executing command with payload {"CounterName":"test","UserId":"user1"} (Command ID: 7583ff816241df09, Trace ID: e2da9ae516ca8503d1dab9ecb6f0625b)
info: UserHistory.Contracts.SetMostRecentlyIncrementedCounterForUserCommand[0]
      Executing command with payload {"UserId":"user1","CounterName":"test"} (Command ID: 660d5a7f49860904, Trace ID: e2da9ae516ca8503d1dab9ecb6f0625b)
info: UserHistory.Contracts.SetMostRecentlyIncrementedCounterForUserCommand[0]
      Executed command in 33.4398ms (Command ID: 660d5a7f49860904, Trace ID: e2da9ae516ca8503d1dab9ecb6f0625b)
info: Counters.Contracts.IncrementCounterCommand[0]
      Executed command and got response {"NewCounterValue":1} in 41.8021ms (Command ID: 7583ff816241df09, Trace ID: e2da9ae516ca8503d1dab9ecb6f0625b)
```

`UserHistory` app:

```log
info: UserHistory.Contracts.SetMostRecentlyIncrementedCounterForUserCommand[0]
      Executing command with payload {"UserId":"user1","CounterName":"test"} (Command ID: 660d5a7f49860904, Trace ID: e2da9ae516ca8503d1dab9ecb6f0625b)
info: UserHistory.Contracts.SetMostRecentlyIncrementedCounterForUserCommand[0]
      Executed command in 8.0365ms (Command ID: 660d5a7f49860904, Trace ID: e2da9ae516ca8503d1dab9ecb6f0625b)
info: UserHistory.Contracts.GetMostRecentlyIncrementedCounterForUserQuery[0]
      Executing query with payload {"UserId":"user1"} (Query ID: 8eca0c0db1c66b35, Trace ID: 03ae6658701e65ae42c8b0975d81850d)
info: UserHistory.Contracts.GetMostRecentlyIncrementedCounterForUserQuery[0]
      Executed query and got response {"CounterName":"test"} in 4.4410ms (Query ID: 8eca0c0db1c66b35, Trace ID: 03ae6658701e65ae42c8b0975d81850d)
```

For the execution of the `SetMostRecentlyIncrementedCounterForUserCommand` you can see the logs on both the client (i.e. `Counters` app) and the server (i.e. `UserHistory` app). Note that the `IncrementCounterCommand` and `SetMostRecentlyIncrementedCounterForUserCommand` have the same trace ID `e2da9ae516ca8503d1dab9ecb6f0625b`, which allows you to correlate log entries (all commands and queries also have their own IDs to correlate log entries for that single command or query invocation).

The final step in the transformation is to adjust the tests for the `Counters` context. In the monolith, the tests for the `IncrementCounterCommand` were using the real `UserHistory` context's command and query handlers. In a distributed system, you have two choices. Firstly, you could mock all commands and queries from other contexts during testing. This is the simplest solution, but it means that you are not testing the integration of the bounded contexts, which may lead to bugs that only show themselves in deployed test or staging environments. The other option is to run the tests against both web apps, i.e. bootstrap both the `Counters` web API and the `UserHistory` web API during test setup, and configure the tests to use both apps. This is the option we are going to explore in this recipe.

First, in the `Counters.Tests.csproj` file, replace the reference to the monolith's entry point with references to both the `Counters.EntryPoint.WebApi` and `UserHistory.EntryPoint.WebApi` projects. Afterwards, replace the contents of `TestBase.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Tests/TestBase.cs)] with the following:

```cs
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.EntryPoint.WebApi;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Contracts;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.EntryPoint.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Tests;

public abstract class TestBase : IDisposable
{
    private readonly ServiceProvider clientServices;
    private readonly WebApplicationFactory<CountersProgram> countersApp;
    private readonly HttpClient httpTestClient;
    private readonly WebApplicationFactory<UserHistoryProgram> userHistoryApp;
    private readonly HttpClient userHistoryClient;

    protected TestBase()
    {
        // bootstrap the UserHistory web app to allow calling its commands and queries during tests
        userHistoryApp = new WebApplicationFactory<UserHistoryProgram>().WithWebHostBuilder(builder =>
        {
            // configure tests to produce logs; this is very useful to debug failing tests
            builder.ConfigureLogging(o => o.ClearProviders()
                                           .AddSimpleConsole()
                                           .SetMinimumLevel(LogLevel.Information));
        });

        userHistoryClient = userHistoryApp.CreateClient();

        countersApp = new WebApplicationFactory<CountersProgram>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(o => o.ClearProviders()
                                           .AddSimpleConsole()
                                           .SetMinimumLevel(LogLevel.Information));

            builder.ConfigureServices(services =>
            {
                services.ConfigureConquerorCQSHttpClientOptions(o =>
                {
                    // configure Conqueror.CQS to use the UserHistory web app's test HTTP client for
                    // all types from the UserHistory's contracts
                    o.UseHttpClientForTypesFromAssembly(typeof(SetMostRecentlyIncrementedCounterForUserCommand).Assembly, userHistoryClient);
                });
            });
        });

        httpTestClient = countersApp.CreateClient();

        // create a dedicated service provider for resolving command and query clients
        // to prevent interference with other services from the server application
        clientServices = new ServiceCollection().AddConquerorCQSHttpClientServices(
                                                    o =>
                                                        // use the test HTTP client for the Counters app for all command and query clients
                                                        o.UseHttpClient(httpTestClient)

                                                         // in our tests we want to call a query from the UserHistory app as part of our test
                                                         // assertions; therefore we also configure the client services to use the UserHistory
                                                         // web app's test HTTP client for all types from the UserHistory's contracts
                                                         .UseHttpClientForTypesFromAssembly(typeof(SetMostRecentlyIncrementedCounterForUserCommand).Assembly, userHistoryClient))
                                                .BuildServiceProvider();
    }

    public void Dispose()
    {
        clientServices.Dispose();
        httpTestClient.Dispose();
        userHistoryClient.Dispose();
        countersApp.Dispose();
        userHistoryApp.Dispose();
    }

    protected T ResolveOnServer<T>()
        where T : notnull => countersApp.Services.GetRequiredService<T>();

    protected T CreateCommandClient<T>()
        where T : class, ICommandHandler => clientServices.GetRequiredService<ICommandClientFactory>().CreateCommandClient<T>(b => b.UseHttp(new("http://localhost")));

    protected T CreateQueryClient<T>()
        where T : class, IQueryHandler => clientServices.GetRequiredService<IQueryClientFactory>().CreateQueryClient<T>(b => b.UseHttp(new("http://localhost")));
}
```

This setup code bootstraps both web applications and configures **Conqueror.CQS** to send commands and queries to those web apps as necessary. The tests themselves don't need any adjustment, they will still work as before. This is one of the awesome aspects of **Conqueror.CQS**, since it gives you the confidence that your system still works as it did in the monolith without requiring the effort of adjusting or rewriting tests.

Lastly, let's discuss how you could separate each bounded context into its own separate code repository (this approach is commonly called "polyrepo"). Performing such a separation gives each context's development team more flexibility and independence, but makes it harder to integrate the different web applications.

The first aspect you need to consider are your contracts. In the monorepo approach, each bounded context can reference other context's contracts with a direct code reference. In a polyrepo approach we recommend that you publish each context's contracts as a NuGet package, which can then be referenced by other contexts.

The second aspect to consider is testing. As we explored above, in a monorepo, we can use ASP.NET Core's test server to bootstrap multiple web applications during the test setup. In a polyrepo, it is more common to mock external dependencies. If you still want to test the integration of the different web applications, this can be done in multiple ways. For example, if your apps are packaged as [containers](https://en.wikipedia.org/wiki/Containerization_(computing)), you could start other apps as a container during testing, and then configure your command and query clients to talk to the containers. Alternatively, you could deploy your apps to a test or staging environments, and then configure your command and query clients to talk to the instances in those environments. Which of these approaches you want to choose depends on the complexity of your application, the size of your team, the maturity of your infrastructure, and other aspects that go beyond the scope of this recipe.

This completes the transformation of our example application into a distributed application. You can view the completed code [here](.completed) in your browser or in the `completed` folder of the recipe's solution.

It is difficult to judge at what point you should consider transforming your monolithic application into a distributed application as discussed in this recipe. You need to consider drivers like horizontal scalability, deployment and development independence, CI pipeline performance, etc. For some projects it makes sense to build them distributed from the start. For other applications you may want to start with a monolith and then refactor your application over the course of its lifetime. Another options is to only extract those modules with specific requirements from the monolith into a separate application, and keep all other modules in the monolith. If and when you decide that you want to make your application distributed, **Conqueror.CQS** helps you by making the transformation as painless as possible, while giving you the flexibility to easily move back to a monolith.

In summary, if you and your team decide to build or transform your application as a distributed application, you need to do the following:

- create new entry points for each module or bounded context
- enable HTTP for your commands and queries as necessary
- in apps with dependencies on other bounded contexts, configure command and query HTTP clients
- adjust your tests to run against the individual web applications instead of the monolithic application

If you want to learn more about using **Conqueror.CQS**, head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.cqs.advanced.monolith-to-distributed]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
