# Conqueror recipe (Messaging): creating a clean architecture and modular monolith with messages

This recipe shows how you can use messages to create a [clean architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) with **Conqueror**.

This is an advanced recipe which builds upon the concepts introduced in the [earlier messaging recipes](../../../../../..#recipes). If you have not yet read those recipes, we recommend you take a look at them before you start with this recipe.

> The discussions in this recipe are accompanied by code examples for the various stages of refactorings we will perform. To explore the code while reading the recipe, [download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/messaging/clean-architecture) and open the solution file in your IDE (note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed).

The application, which we will refactor into a clean architecture, is managing a set of named counters which can be incremented by users. The application also tracks a history for which counter was incremented by which user and allows fetching the most recently incremented counter for a given user. In code, the API of our application is represented with the following types:

```cs
[HttpMessage<IncrementCounterResponse>(HttpMethod = "POST")]
public partial record IncrementCounter(string CounterName, string UserId);

public record IncrementCounterResponse(int NewCounterValue);

[HttpMessage<GetCounterValueResponse>(HttpMethod = "GET")]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);

[HttpMessage<GetMostRecentlyIncrementedCounterForUserResponse>(HttpMethod = "GET")]
public partial record GetMostRecentlyIncrementedCounterForUser(string UserId);

public record GetMostRecentlyIncrementedCounterForUserResponse(string? CounterName);
```

> Each message type is a `partial record` because Conqueror's source generator adds a nested `IHandler` (and `IPipeline`) interface to it. This means you no longer need to declare a handler interface by hand: `IncrementCounter.IHandler` is generated for you. Whether a message reads or writes data is just a convention now, so both are modelled with the same `[Message]`/`[HttpMessage]` attribute; only the HTTP method (`GET` for reads, `POST` for writes) reflects the distinction.

The application consists of a single implementation project as well as a single test project. Over the course of this recipe we are going to extract certain pieces of code into separate projects to clearly separate concerns from each other according to the idea of a [clean architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html). We are also going to apply the concepts of [domain-driven design](https://en.wikipedia.org/wiki/Domain-driven_design) to split our application into different bounded contexts and thereby make the application into a [modular monolith](https://martinfowler.com/bliki/MonolithFirst.html).

> We also have a recipe for [refactoring a modular monolith into a distributed application](../monolith-to-distributed#readme), which you can take a look at after reading through this recipe.

Let's first talk about when and why you would want to use a clean architecture. Many applications start out small and grow over time as more and more features are added. Initially, the application may be just fine being implemented in a single project with not much structure. However, once such an application grows beyond a certain point, certain pains are often experienced. For example, the business logic might be too tightly coupled to the database, making it difficult to test. Or there may be too many dependencies in all directions, making it difficult to track the flow of data and logic through the application. There are other drivers as well, many of which are discussed in [this excellent blog post](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html).

In this recipe we assume that a team started building the example application with **Conqueror** and is now experiencing some of those pains of a growing application (which is of course not yet the case for our simple application, but it still serves as a good illustration). Therefore, the team wants to refactor the application into a clean architecture.

> There are many ways to build a clean architecture in .NET, and we're going to look at one such way. We are also going to use some of the [SOLID principles](https://en.wikipedia.org/wiki/SOLID) of object-oriented design as well as some other related concepts. You can consider this recipe to be quite an opinionated take on clean architectures, which can serve as an illustration of the general ideas, but it shouldn't be copied verbatim. You or your team may (and probably should) choose different names, layers, and file structures than what we look at here, depending on the concrete application or system you are building.

One core idea of a clean architecture is to separate your core business logic from aspects like HTTP, databases, message queues, etc. This idea fits very well with **Conqueror**'s approach of making your message handlers transport-agnostic. For our clean architecture, we are going to separate our application into three layers: application, infrastructure, and the web API entry point. The application layer contains our application business logic, which means our message handlers. The infrastructure layer will contain the [repositories](https://martinfowler.com/eaaCatalog/repository.html), which interact with a database (although in our simple example application, everything is just stored in-memory). The web API entry point layer will contain all the setup code to run a web server. In a typical project this would include controllers, but as you saw in the recipe for [exposing messages via HTTP](../../../../transports/http/recipes/messaging/exposing-via-http#readme), **Conqueror** dynamically maps the HTTP endpoints for your messages, meaning that this layer can be very thin.

> In more complex applications you could consider introducing a separate layer which contains your domain objects, as well as any business logic calculations. In such a setup the message handlers in the application layer become orchestrators which call into the domain layer instead of implementing the business logic themselves.

Before we start with the refactoring, we'll think about our desired file structure on paper. Our starting point can be seen [here](.starting-point) in your browser or in the `.starting-point` folder in the recipe's solution. Here is a simplified view of the file structure (omitting irrelevant files like `Usings.cs`):

```txt
Conqueror.Recipes.Messaging.CleanArchitecture/
├── CountersRepository.cs
├── DefaultPipelines.cs
├── GetCounterValue.cs
├── GetMostRecentlyIncrementedCounterForUser.cs
├── IncrementCounter.cs
├── Program.cs
└── UserHistoryRepository.cs
Conqueror.Recipes.Messaging.CleanArchitecture.Tests/
├── GetCounterValueTests.cs
├── GetMostRecentlyIncrementedCounterForUserTests.cs
└── IncrementCounterTests.cs
```

> Note that thanks to testing our application through its public API (i.e. its messages) as discussed in the recipe for [testing message handlers](../testing-handlers#readme), we are able to make some significant changes to the implementation with only minor adjustments to the tests. It is often difficult to make such big refactorings in an application with a lot of low-level unit tests, since those tests also require a lot of adjustments during the refactoring, taking up valuable development time.

If we simply move the files into different layers as outlined above, we'll get the following structure (omitting the `Conqueror.Recipes.Messaging.CleanArchitecture` project name prefix for simplicity):

```txt
Application/
├── DefaultPipelines.cs
├── GetCounterValue.cs
├── GetMostRecentlyIncrementedCounterForUser.cs
└── IncrementCounter.cs
Infrastructure/
├── CountersRepository.cs
└── UserHistoryRepository.cs
EntryPoint.WebApi/
└── Program.cs
Tests/
├── GetCounterValueTests.cs
├── GetMostRecentlyIncrementedCounterForUserTests.cs
└── IncrementCounterTests.cs
```

In this structure, the `EntryPoint.WebApi` project will reference the `Application` and `Infrastructure` projects, the `Infrastructure` project will reference the `Application` project, and the `Tests` project references all other projects.

> With this structure, it is very easy to add additional entry points to your application. For example, if there was a requirement to allow calling the `IncrementCounter` message via a message queue in addition to a web API, you could just create an `EntryPoint.QueueApi` project, that uses the exact same `Application` and `Infrastructure` projects.

However, this won't work yet, since the message handlers require a reference to the repositories. To solve this, we can use the [dependency inversion principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle) by introducing interfaces for the repositories:

```diff
  Application/
  ├── DefaultPipelines.cs
  ├── GetCounterValue.cs
  ├── GetMostRecentlyIncrementedCounterForUser.cs
+ ├── ICountersRepository.cs
  ├── IncrementCounter.cs
+ └── IUserHistoryRepository.cs
  Infrastructure/
  ├── CountersRepository.cs
  └── UserHistoryRepository.cs
  EntryPoint.WebApi/
  └── Program.cs
  Tests/
  ├── GetCounterValueTests.cs
  ├── GetMostRecentlyIncrementedCounterForUserTests.cs
  └── IncrementCounterTests.cs
```

Creating a single interface for each repository is a common approach that works well in many cases. However, in accordance with the spirit of [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation) as well as according to the [interface segregation principle](https://en.wikipedia.org/wiki/Interface_segregation_principle), we can go one step further and split the repository interfaces into two interfaces each: one for reading and one for writing.

```diff
  Application/
  ├── DefaultPipelines.cs
  ├── GetCounterValue.cs
  ├── GetMostRecentlyIncrementedCounterForUser.cs
- ├── ICountersRepository.cs
+ ├── ICountersReadRepository.cs
+ ├── ICountersWriteRepository.cs
  ├── IncrementCounter.cs
- ├── IUserHistoryRepository.cs
+ ├── IUserHistoryReadRepository.cs
+ └── IUserHistoryWriteRepository.cs
  Infrastructure/
  ├── CountersRepository.cs
  └── UserHistoryRepository.cs
  EntryPoint.WebApi/
  └── Program.cs
  Tests/
  ├── GetCounterValueTests.cs
  ├── GetMostRecentlyIncrementedCounterForUserTests.cs
  └── IncrementCounterTests.cs
```

This is the structure we will use in this recipe as the basis for further refactorings. You can view it [here](.layers) in your browser or in the `.layers` folder in the recipe's solution.

Before we move on to the next part of the refactoring, let's briefly discuss one further step you could take in regards to repositories in more complex applications. In those kinds of applications your message handlers may require complex database interactions, which would not be suitable for the domain-object-centric read and write repository interfaces we used above. Instead, you can create a dedicated repository interface for each handler which contains all the database operations which are required by that handler. This approach allows isolating handlers completely from each other. Implementing this approach is left as an exercise for the reader. The file structure could look like this:

```txt
Application/
├── DefaultPipelines.cs
├── GetCounterValue/
│   ├── GetCounterValue.cs
│   └── IGetCounterValueRepository.cs
├── GetMostRecentlyIncrementedCounterForUser/
│   ├── GetMostRecentlyIncrementedCounterForUser.cs
│   └── IGetMostRecentlyIncrementedCounterForUserRepository.cs
└── IncrementCounter/
    ├── IIncrementCounterRepository.cs
    └── IncrementCounter.cs
```

The layered project structure discussed above is suitable for many applications and scales quite well. However, when your application reaches a certain size or complexity, you may want to consider a further separation (and sometimes you may even want to do this separation from the start). One way to do this is to use [domain-driven design](https://en.wikipedia.org/wiki/Domain-driven_design) and split your application into separate [bounded contexts](https://martinfowler.com/bliki/BoundedContext.html).

In this recipe, we are going to split our example application into two bounded contexts: `Counters` and `UserHistory`. For each of these contexts we are going to create an `Application`, an `Infrastructure`, and a `Tests` project. However, since we are building this application as a [modular monolith](https://martinfowler.com/bliki/MonolithFirst.html), we are going to keep a single `EntryPoint.WebApi` project. We also would like to share our default message pipelines across all bounded contexts, and therefore we place them in a shared project called `Core.Application`.

> If there are other aspects you want to share across different bounded contexts, you can introduce additional projects as necessary. For example, if you have common code for the `Infrastructure` layer, you could create a `Core.Infrastructure` project, etc. (The prefix `Core` is one option among many others like `Platform`, `Base`, etc.; discuss with your team which one you prefer for your specific application).

The folder structure of our monolith could look like this:

```txt
Core.Application/
└── DefaultPipelines.cs
Counters.Application/
├── GetCounterValue.cs
├── ICountersReadRepository.cs
├── ICountersWriteRepository.cs
└── IncrementCounter.cs
Counters.Infrastructure/
└── CountersRepository.cs
Counters.Tests/
├── GetCounterValueTests.cs
└── IncrementCounterTests.cs
UserHistory.Application/
├── GetMostRecentlyIncrementedCounterForUser.cs
├── IUserHistoryReadRepository.cs
└── IUserHistoryWriteRepository.cs
UserHistory.Infrastructure/
└── UserHistoryRepository.cs
UserHistory.Tests/
└── GetMostRecentlyIncrementedCounterForUserTests.cs
EntryPoint.WebApi/
└── Program.cs
```

In this structure, the `EntryPoint.WebApi` project will reference both context's `Application` and `Infrastructure` projects, each `Infrastructure` project will reference its context's `Application` project, each `Application` project references the `Core.Application` project, and the `Tests` projects reference all other projects.

> While each bounded context has its own `Tests` project, the tests still bootstrap the whole application, since we want to test the messages from each context with full integration with other contexts.

There is still one open question: the `UserHistory` context needs to know when a counter was incremented, but how can the `Counters` context communicate this? There are a few options to do this:

- we could add logic in the `Counters` context for writing to the `UserHistory` database directly (this works but violates the separation of the contexts)
- we could add a dependency from the `Counters.Application` project to the `UserHistory.Infrastructure` project, and use the `IUserHistoryWriteRepository` to update the user's history (this works, but leads to tight coupling between the contexts, which hurts long term maintainability)
- we can introduce a new message in the `UserHistory` context for updating the user history (this is the approach we will use in this recipe)
- we can use [Conqueror signals](../../signalling/getting-started#readme) to publish a domain signal from the `Counters` context and have a signal handler in the `UserHistory` context react to it (a recipe exploring how our example application could look like using a signal instead of a message can be found [here](../../signalling/getting-started#readme))

We introduce a new `SetMostRecentlyIncrementedCounterForUser` message which can be called by the `Counters` context whenever a counter is incremented. However, as it stands we would still require a reference from `Counters.Application` to `UserHistory.Application` in order to send the new message, which still leads to undesired coupling. Instead of this direct dependency, we can use the [dependency inversion principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle) once again and extract our message types into separate `Contracts` projects, which allow one context to send messages from another context while maintaining loose coupling. Even though, strictly speaking, we only need this for our new message, we are going to do this for all messages of both contexts for consistency. The folder structure would be adjusted like this:

```diff
  Core.Application/
  └── DefaultPipelines.cs
  Counters.Application/
- ├── GetCounterValue.cs
+ ├── GetCounterValueHandler.cs
  ├── ICountersReadRepository.cs
  ├── ICountersWriteRepository.cs
- ├── IncrementCounter.cs
+ └── IncrementCounterHandler.cs
+ Counters.Contracts/
+ ├── GetCounterValue.cs
+ └── IncrementCounter.cs
  Counters.Infrastructure/
  └── CountersRepository.cs
  Counters.Tests/
  ├── GetCounterValueTests.cs
  └── IncrementCounterTests.cs
  UserHistory.Application/
- ├── GetMostRecentlyIncrementedCounterForUser.cs
+ ├── GetMostRecentlyIncrementedCounterForUserHandler.cs
  ├── IUserHistoryReadRepository.cs
  ├── IUserHistoryWriteRepository.cs
+ └── SetMostRecentlyIncrementedCounterForUserHandler.cs
+ UserHistory.Contracts/
+ ├── GetMostRecentlyIncrementedCounterForUser.cs
+ └── SetMostRecentlyIncrementedCounterForUser.cs
  UserHistory.Infrastructure/
  └── UserHistoryRepository.cs
  UserHistory.Tests/
  └── GetMostRecentlyIncrementedCounterForUserTests.cs
  EntryPoint.WebApi/
  └── Program.cs
```

The split into `Contracts` and `Application` projects maps naturally onto Conqueror's model: the message type (with its generated `IHandler` interface) lives in the `Contracts` project, while the handler class that implements the interface lives in the `Application` project. A context that wants to call another context's message only references that context's `Contracts` project.

The new `SetMostRecentlyIncrementedCounterForUser` message is an internal contract, so it does not carry an `[HttpMessage]` attribute (we don't want to expose it to the outside world); a plain `[Message]` is enough. The `Counters` context sends it through the injected `IMessageSenders` ([view completed file](.completed/Conqueror.Recipes.Messaging.CleanArchitecture.Counters.Application/IncrementCounterHandler.cs)):

```cs
internal partial class IncrementCounterHandler(
    ICountersReadRepository countersReadRepository,
    ICountersWriteRepository countersWriteRepository,
    IMessageSenders senders)
    : IncrementCounter.IHandler
{
    public static void ConfigurePipeline(IncrementCounter.IPipeline pipeline) => pipeline.UseDefault();

    public async Task<IncrementCounterResponse> Handle(IncrementCounter message, CancellationToken cancellationToken = default)
    {
        var counterValue = await countersReadRepository.GetCounterValue(message.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await countersWriteRepository.SetCounterValue(message.CounterName, newCounterValue);

        // integrate with the UserHistory context through its contract, without a direct dependency on its implementation
        await senders.For(SetMostRecentlyIncrementedCounterForUser.T)
                     .Handle(new SetMostRecentlyIncrementedCounterForUser(message.UserId, message.CounterName), cancellationToken);

        return new IncrementCounterResponse(newCounterValue);
    }
}
```

In this structure, the `Counters.Application` project references the `UserHistory.Contracts` project and the `EntryPoint.WebApi` project takes care of all the bootstrapping to ensure all messages from all context contracts can be sent.

This completes the refactoring of our example application into a clean architecture. You can view the completed code [here](.completed) in your browser or in the `.completed` folder of the recipe's solution.

You can run the completed application with the following command and explore its endpoints (e.g. via the Swagger UI at `http://localhost:5000/swagger`):

```sh
dotnet run --project .completed/Conqueror.Recipes.Messaging.CleanArchitecture.EntryPoint.WebApi
```

For example, incrementing a counter and then fetching the user's most recently incremented counter shows the two bounded contexts working together:

```sh
curl http://localhost:5000/api/incrementCounter -H 'Content-Type: application/json' -d '{"counterName":"test","userId":"user1"}'
# {"newCounterValue":1}

curl 'http://localhost:5000/api/getMostRecentlyIncrementedCounterForUser?userId=user1'
# {"counterName":"test"}
```

It is difficult to judge at what point you should consider transforming your application into a clean architecture or separating it into bounded contexts as discussed in this recipe. For some projects it makes sense to do so from the start. For other applications you may want to start simple and then refactor your application over the course of its lifetime. In any case, **Conqueror** helps you by providing a natural separation of code and context boundaries through your messages.

In summary, if you and your team decide to build or refactor your application as a clean architecture, you need to do the following:

- extract your message types into a contracts project
- extract your message handlers into an infrastructure-agnostic application project
- extract your infrastructure components like repositories into an infrastructure project
- separate your bounded contexts into independent projects (add new messages as necessary)
- integrate your bounded contexts through their contracts without a direct dependency

As the next step we recommend that you explore how to refactor the modular monolith we built in this recipe into a [distributed application](../monolith-to-distributed#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.clean-architecture]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
