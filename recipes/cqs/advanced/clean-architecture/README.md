# Conqueror recipe (CQS Advanced): creating a clean architecture with commands and queries

This recipe shows how you can use commands and queries to create a [clean architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) with **Conqueror.CQS**.

This is an advanced recipe which builds upon the concepts introduced in the [recipes about CQS basics](../../../../../..#cqs-basics). If you have not yet read those recipes, we recommend you take a look at them before you start with this recipe.

> The discussions in this recipe are accompanied by code examples for the various stages of refactorings we will perform. To explore the code while reading the recipe, [download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/clean-architecture) and open the solution file in your IDE (note that you need to have [.NET 6 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed).

The application, which we will refactor into a clean architecture, is managing a set of named counters with can be incremented by users. The application also tracks a history for which counter was incremented by which user and allows fetching the most recently incremented counter for a given user. In code, the API of our application is represented with the following types:

```cs
[HttpCommand]
public sealed record IncrementCounterCommand([Required] string CounterName, [Required] string UserId);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

[HttpQuery]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);

[HttpQuery]
public sealed record GetMostRecentlyIncrementedCounterForUserQuery([Required] string UserId);

public sealed record GetMostRecentlyIncrementedCounterForUserQueryResponse(string? CounterName);
```

The application consists of a single implementation project as well as a single test project. Over the course of this recipe we are going to extract certain pieces of code into separate projects to clearly separate concerns from each other according to the idea of a [clean architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html). We are also going to apply the concepts of [domain-driven design](https://en.wikipedia.org/wiki/Domain-driven_design) to split our application into different bounded contexts and thereby make the application into a [modular monolith](https://martinfowler.com/bliki/MonolithFirst.html).

> We also have a recipe for [refactoring a modular monolith into a distributed application](../monolith-to-distributed#readme), which you can take a look at after reading through this recipe.

Let's first talk about when and why you would want to use a clean architecture. Many applications start out small and grow over time as more and more features are added. Initially, the application may be just fine being implemented in a single project with not much structure. However, once such an application grows beyond a certain point, certain pains are often experienced. For example, the business logic might be too tigthtly coupled to the database, making it difficult to test. Or there may be too many dependencies in all directions, making it difficult to track the flow of data and logic through the application. There are other drivers as well, many of which are discussed in [this excellent blog post](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html).

In this recipe we assume that a team started building the example application with **Conqueror.CQS** and is now experiencing some of those pains of a growing application (which is of course not yet the case for our simple application, but it still serves as a good illustration). Therefore, the team wants to refactor the application into a clean architecture.

> There are many ways to build a clean architecture in .NET, and we're going to look at one such way. We are also going to use some of the [SOLID principles](https://en.wikipedia.org/wiki/SOLID) of object-oriented design as well as some other related concepts. You can consider this recipe to be quite an opinionated take on clean architectures, which can serve as an illustration of the general ideas, but it shouldn't be copied verbatim. You or your team may (and probably should) choose different names, layers, and file structures than what we look at here, depending on the concrete application or system you are building.

One core idea of a clean architecture is to separate your core business logic from aspects like HTTP, databases, message queues, etc. This idea fits very well with **Conqueror**'s approach of making your command and query handlers transport-agnostic. For our clean architecture, we are going to separate our application into three layers: application, infrastructure, and web. The application layer contains our application business logic, which means our command and query handlers. The infrastructure layer will contain the [repositories](https://martinfowler.com/eaaCatalog/repository.html), which interact with a database (although in our simple example application, everything is just stored in-memory). The web layer will contain all the setup code to run a web server. In a typical project this would include controllers, but as you saw in the recipe for [exposing commands and queries via HTTP](../exposing-via-http#readme), **Conqueror.CQS** dynamically creates the controllers for your commands and queries, meaning that the web layer can be very thin.

> In more complex applications you could consider introducing a separate layer which contains your domain objects, as well as any business logic calculations. In such a setup the command and query handlers in the application layer become orchestrators which call into the domain layer instead of implementing the business logic themselves.

Before we start with the refactoring, we'll think about our desired file structure on paper. Our starting point can be seen [here](.starting-point) in your browser or in the `starting-point` folder in the recipe's solution. Here is a simplified view of the file structure (omitting irrelevant files like `Usings.cs`):

```txt
Conqueror.Recipes.CQS.Advanced.CleanArchitecture/
├── CountersRepository.cs
├── GetCounterValueQuery.cs
├── GetMostRecentlyIncrementedCounterForUserQuery.cs
├── IncrementCounterCommand.cs
├── Program.cs
└── UserHistoryRepository.cs
Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Tests/
├── GetCounterValueQueryTests.cs
├── GetMostRecentlyIncrementedCounterForUserQueryTests.cs
└── IncrementCounterCommandTests.cs
```

> Note that thanks to testing our application through its public API (i.e. its commands and queries) as discussed in the recipe for [testing command and query handlers](../../basics/testing-handlers#readme), we are able to make some significant changes to the implementation without needing to adjust any tests. It is often difficult to make such big refactorings in an application with a lot of low-level unit tests, since the tests also need to be adjusted during the refactoring, taking up valuable development time.

If we simply move the files into different layers as outlined above, we'll get the following structure (omitting the `Conqueror.Recipes.CQS.Advanced.CleanArchitecture` project name prefix for simplicity):

```txt
Application/
├── GetCounterValueQuery.cs
├── GetMostRecentlyIncrementedCounterForUserQuery.cs
└── IncrementCounterCommand.cs
Infratructure/
├── CountersRepository.cs
└── UserHistoryRepository.cs
Tests/
├── GetCounterValueQueryTests.cs
├── GetMostRecentlyIncrementedCounterForUserQueryTests.cs
└── IncrementCounterCommandTests.cs
Web/
└── Program.cs
```

In this structure, the `Web` project will reference the `Application` and `Infrastructure` projects, the `Infrastructure` project will reference the `Application` project, and the `Tests` project references all other projects. However, this won't work yet, since the command and query handlers require a reference to the repositories. To solve this, we can use the [dependency inversion principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle) by introducing interfaces for the repositories:

```diff
  Application/
  ├── GetCounterValueQuery.cs
  ├── GetMostRecentlyIncrementedCounterForUserQuery.cs
+ ├── ICountersRepository.cs
  ├── IncrementCounterCommand.cs
+ └── IUserHistoryRepository.cs
  Infratructure/
  ├── CountersRepository.cs
  └── UserHistoryRepository.cs
  Tests/
  ├── GetCounterValueQueryTests.cs
  ├── GetMostRecentlyIncrementedCounterForUserQueryTests.cs
  └── IncrementCounterCommandTests.cs
  Web/
  └── Program.cs
```

Creating a single interface for each repository is a common approach that works well in many cases. However, in accordance with the spirit of [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation) as well as according to the [interface segregation principle](https://en.wikipedia.org/wiki/Interface_segregation_principle), we can go one step further and split the repository interfaces into two interfaces each: one for reading and one for writing.

```diff
  Application/
  ├── GetCounterValueQuery.cs
  ├── GetMostRecentlyIncrementedCounterForUserQuery.cs
- ├── ICountersRepository.cs
+ ├── ICountersReadRepository.cs
+ ├── ICountersWriteRepository.cs
  ├── IncrementCounterCommand.cs
- ├── IUserHistoryRepository.cs
+ ├── IUserHistoryReadRepository.cs
+ └── IUserHistoryWriteRepository.cs
  Infratructure/
  ├── CountersRepository.cs
  └── UserHistoryRepository.cs
  Tests/
  ├── GetCounterValueQueryTests.cs
  ├── GetMostRecentlyIncrementedCounterForUserQueryTests.cs
  └── IncrementCounterCommandTests.cs
  Web/
  └── Program.cs
```

This is the structure we will use in this recipe as the basis for further refactorings. You can view it [here](.layers) in your browser or in the `layers` folder in the recipe's solution.

Before we move on to the next part of the refactoring, let's briefly discuss one further step you could take in regards to repositories in more complex applications. In those kinds of applications your command and query handlers may require complex database interactions, which would not be suitable for the domain-object-centric read and write repository interfaces we used above. Instead, you can create a dedicated repository interface for each handler which contains all the database operations which are required by that handler. This approach allows isolating handlers completely from each other. Implementing this approach is left as an exercise for the reader. The file structure could look like this:

```txt
Application/
├── GetCounterValueQuery/
│   ├── GetCounterValueQuery.cs
│   └── IGetCounterValueQueryRepository.cs
├── GetMostRecentlyIncrementedCounterForUserQuery/
│   ├── GetMostRecentlyIncrementedCounterForUserQuery.cs
│   └── IGetMostRecentlyIncrementedCounterForUserQueryRepository.cs
└── IncrementCounterCommand/
    ├── IIncrementCounterCommandRepository.cs
    └── IncrementCounterCommand.cs
```

The layered project structure discussed above is suitable for many applications and scales quite well. However, when your application reaches a certain size or complexity, you may want to consider a further separation (and sometimes you may even want to do this separation from the start). One way to do this is to use [domain-driven design](https://en.wikipedia.org/wiki/Domain-driven_design) and split your application into separate [bounded contexts](https://martinfowler.com/bliki/BoundedContext.html).

In this recipe, we are going to split our example application into two bounded contexts: `Counters` and `UserHistory`. For each of these contexts we are going to create an `Application` and an `Infrastructure` project. However, since we are building this application as a [modular monolith](https://martinfowler.com/bliki/MonolithFirst.html), we are going to keep a single `Web` project and a single `Tests` project.

> There is a recipe, in which we explore how this modular monolith can be [refactored into a distributed application](../monolith-to-distributed#readme).

The folder structure of our monolith could look like this:

```txt
Counters.Application/
├── GetCounterValueQuery.cs
├── ICountersReadRepository.cs
├── ICountersWriteRepository.cs
└── IncrementCounterCommand.cs
Counters.Infratructure/
└── CountersRepository.cs
UserHistory.Application/
├── GetMostRecentlyIncrementedCounterForUserQuery.cs
├── IUserHistoryReadRepository.cs
└── IUserHistoryWriteRepository.cs
UserHistory.Infratructure/
├── CountersRepository.cs
└── UserHistoryRepository.cs
Tests/
├── GetCounterValueQueryTests.cs
├── GetMostRecentlyIncrementedCounterForUserQueryTests.cs
└── IncrementCounterCommandTests.cs
Web/
└── Program.cs
```

In this structure, the `Web` project will reference both context's `Application` and `Infrastructure` projects, each `Infrastructure` project will reference its context's `Application` project, and the `Tests` project references all other projects. But there is one open question: the `UserHistory` context needs to know when a counter was incremented, but how can the `Counters` context communicate this? There are a few options to do this:

- we could add logic in the `Counters` context for writing to the `UserHistory` database directly (this works but violates the separation of the contexts)
- we could add a dependency from the `Counters.Application` project to the `UserHistory.Infratructure` project, and use the `IUserHistoryWriteRepository` to update the user's history (this works, but leads to tight coupling between the contexts, which hurts long term maintainability)
- we can introduce a new command in the `UserHistory` context for updating the user history (this is the approach we will use in this recipe)
- we can use the [Conqueror.Eventing](../../../../../..#conqueroreventing) library to publish a domain event from the `Counters` context and have an event observer in the `UserHistory` context (the [Conqueror.Eventing](../../../../../..#conqueroreventing) library is still experimental, but once it is stable you will be able to find a recipe [here](../../../eventing/advanced/clean-architecture#readme) which explores how our example application could look like using an event instead of a command)

We introduce a new `SetMostRecentlyIncrementedCounterForUserCommand` which can be called by the `Counters` context whenever a counter is incremented. However, as it stands we would still require a reference from `Counters.Application` to `UserHistory.Application` in order to call the new command, which still leads to undesired coupling. Instead of this direct dependency, we can use the [dependency inversion principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle) once again and extract our command and query types into separate `Contracts` projects, which allow one context to call commands and queries from another context while maintaining loose coupling. Even though, strictly speaking, we only need this for our new command, we are going to do this for all commands and queries of both contexts for consistency. The folder structure would be adjusted like this:

```diff
  Counters.Application/
- ├── GetCounterValueQuery.cs
+ ├── GetCounterValueQueryHandler.cs
  ├── ICountersReadRepository.cs
  ├── ICountersWriteRepository.cs
- ├── IncrementCounterCommand.cs
+ └── IncrementCounterCommandHandler.cs
+ Counters.Contracts/
+ ├── GetCounterValueQuery.cs
+ └── IncrementCounterCommand.cs
  Counters.Infratructure/
  └── CountersRepository.cs
  UserHistory.Application/
- ├── GetMostRecentlyIncrementedCounterForUserQuery.cs
+ ├── GetMostRecentlyIncrementedCounterForUserQueryHandler.cs
  ├── IUserHistoryReadRepository.cs
  ├── IUserHistoryWriteRepository.cs
+ └── SetMostRecentlyIncrementedCounterForUserCommandHandler.cs
+ UserHistory.Contracts/
+ ├── GetMostRecentlyIncrementedCounterForUserQuery.cs
+ └── SetMostRecentlyIncrementedCounterForUserCommand.cs
  UserHistory.Infratructure/
  ├── CountersRepository.cs
  └── UserHistoryRepository.cs
  Tests/
  ├── GetCounterValueQueryTests.cs
  ├── GetMostRecentlyIncrementedCounterForUserQueryTests.cs
  └── IncrementCounterCommandTests.cs
  Web/
  └── Program.cs
```

In this structure, the `Counters.Application` project references the `UserHistory.Contracts` project and the `Web` project takes care of all the bootstrapping to ensure all commands and queries from all context contracts can be called.

> If there are other aspects you want to share across different bounded contexts, you can introduce additional projects as necessary. For example, if you have custom middlewares, those could be placed into a separate `Middlewares` project, which is referenced from all context's `Application` projects. If you have shared code for any of the layers discussed above, you could also consider introducing shared project per layer, for example `Core.Application`, `Core.Infrastructure`, etc. (The prefix `Core` is one option among many others like `Platform`, `Base`, etc.; discuss with your team which one your prefer for your specific application).

This completes the refactoring of our example application into a clean architecture. You can view the completed code [here](.completed) in your browser or in the `completed` folder of the recipe's solution.

It is difficult to judge at what point you should consider transforming your application into a clean architecture or separating it into bounded contexts as discussed in this recipe. For some projects it makes sense to do so from the start. For other applications you may want to start simple and then refactor your application over the course of its lifetime. In any case, **Conqueror.CQS** helps you by providing a natural separation of code and context boundaries through your commands and queries.

In summary, if you and your team decide to build or refactor your application as a clean architecture, you need to do the following:

- extract your command and query types into a contracts project
- extract your command and query handlers into an infrastructure-agnostic application project
- extract your infrastructure components like repositories into an infrastructure project
- separate your bounded contexts into independent projects (add new commands and queries as necessary)
- integrate your bounded contexts through their contracts without a direct dependency

As the next step we recommend that you explore how to refactor the modular monolith we built in this recipe into a [distributed application](../monolith-to-distributed#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.cqs.advanced.clean-architecture]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
