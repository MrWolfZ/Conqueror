# Conqueror - for building scalable & maintainable .NET applications

> ATTENTION: This project is currently still undergoing active development and contrary to what some of this README says, everything in here is still subject to change. Therefore please do not yet use this project for any production application.

**Conqueror** is a set of libraries that helps you build .NET applications in a structured way (using patterns like [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation), [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) (often also known as middlewares), [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern), [data streams](https://en.wikipedia.org/wiki/Data_stream), etc.), while keeping them scalable (both from the development perspective as well as at runtime).

See our [quickstart](#quickstart) or [example projects](examples) if you want to jump right into code examples for using **Conqueror**. Or head over to our [recipes](#recipes) for more detailed guidance on how you can utilize **Conqueror** to its maximum. Finally, if you want to learn more about the motivation behind this project (including comparisons to similar projects like [MediatR](https://github.com/jbogard/MediatR)), head over to the [motivation](#motivation) section.

[![Build Status](https://github.com/MrWolfZ/Conqueror/actions/workflows/dotnet.yml/badge.svg)](https://github.com/MrWolfZ/Conqueror/actions/workflows/dotnet.yml)
[![license](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

> **Conqueror** only supports .NET 6+

## Libraries

**Conqueror.CQS (_stable_)**: Split your business processes into simple-to-maintain and easy-to-test pieces of code using the [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation) pattern. Handle cross-cutting concerns like logging, validation, authorization etc. using configurable middlewares. Keep your applications scalable by moving commands and queries from a modular monolith to a distributed application with minimal friction.

Head over to our [CQS recipes](#cqs-introduction) for more guidance on how to use this library.

[![NuGet version (Conqueror.CQS)](https://img.shields.io/nuget/v/Conqueror.CQS?label=Conqueror.CQS)](https://www.nuget.org/packages/Conqueror.CQS/)
[![NuGet version (Conqueror.CQS.Abstractions)](https://img.shields.io/nuget/v/Conqueror.CQS.Abstractions?label=Conqueror.CQS.Abstractions)](https://www.nuget.org/packages/Conqueror.CQS.Abstractions/)
[![NuGet version (Conqueror.CQS.Analyzers)](https://img.shields.io/nuget/v/Conqueror.CQS.Analyzers?label=Conqueror.CQS.Analyzers)](https://www.nuget.org/packages/Conqueror.CQS.Analyzers/)
[![NuGet version (Conqueror.CQS.Middleware.Logging)](https://img.shields.io/nuget/v/Conqueror.CQS.Middleware.Logging?label=Conqueror.CQS.Middleware.Logging)](https://www.nuget.org/packages/Conqueror.CQS.Middleware.Logging/)
[![NuGet version (Conqueror.CQS.Transport.Http.Server.AspNetCore)](https://img.shields.io/nuget/v/Conqueror.CQS.Transport.Http.Server.AspNetCore?label=Conqueror.CQS.Transport.Http.Server.AspNetCore)](https://www.nuget.org/packages/Conqueror.CQS.Transport.Http.Server.AspNetCore/)
[![NuGet version (Conqueror.CQS.Transport.Http.Client)](https://img.shields.io/nuget/v/Conqueror.CQS.Transport.Http.Client?label=Conqueror.CQS.Transport.Http.Client)](https://www.nuget.org/packages/Conqueror.CQS.Transport.Http.Client/)

**Conqueror.Eventing (_experimental_)**: Decouple your application logic by using in-process event publishing using the [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) pattern. Handle cross-cutting concerns like logging, tracing, filtering etc. using configurable middlewares.

Head over to our [eventing recipes](#eventing-introduction) for more guidance on how to use this library.

[![NuGet version (Conqueror.Eventing)](https://img.shields.io/nuget/v/Conqueror.Eventing?label=Conqueror.Eventing)](https://www.nuget.org/packages/Conqueror.Eventing/)
[![NuGet version (Conqueror.Eventing.Abstractions)](https://img.shields.io/nuget/v/Conqueror.Eventing.Abstractions?label=Conqueror.Eventing.Abstractions)](https://www.nuget.org/packages/Conqueror.Eventing.Abstractions/)

**Conqueror.Streaming.Interactive (_experimental_)**: Keep your applications in control by allowing them to consume [data streams](https://en.wikipedia.org/wiki/Data_stream) at their own pace using a pull-based interactive approach. Handle cross-cutting concerns like logging, error handling, authorization etc. using configurable middlewares. Keep your applications scalable by moving stream consumers from a modular monolith to a distributed application with minimal friction.

Head over to our [interactive streaming recipes](#interactive-streaming-introduction) for more guidance on how to use this library.

[![NuGet version (Conqueror.Streaming.Interactive)](https://img.shields.io/nuget/v/Conqueror.Streaming.Interactive?label=Conqueror.Streaming.Interactive)](https://www.nuget.org/packages/Conqueror.Streaming.Interactive/)
[![NuGet version (Conqueror.Streaming.Interactive.Abstractions)](https://img.shields.io/nuget/v/Conqueror.Streaming.Interactive.Abstractions?label=Conqueror.Streaming.Interactive.Abstractions)](https://www.nuget.org/packages/Conqueror.Streaming.Interactive.Abstractions/)
[![NuGet version (Conqueror.Streaming.Interactive.Transport.Http.Server.AspNetCore)](https://img.shields.io/nuget/v/Conqueror.Streaming.Interactive.Transport.Http.Server.AspNetCore?label=Conqueror.Streaming.Interactive.Transport.Http.Server.AspNetCore)](https://www.nuget.org/packages/Conqueror.Streaming.Interactive.Transport.Http.Server.AspNetCore/)
[![NuGet version (Conqueror.Streaming.Interactive.Transport.Http.Client)](https://img.shields.io/nuget/v/Conqueror.Streaming.Interactive.Transport.Http.Client?label=Conqueror.Streaming.Interactive.Transport.Http.Client)](https://www.nuget.org/packages/Conqueror.Streaming.Interactive.Transport.Http.Client/)

**Conqueror.Streaming.Reactive (_early prototype_)**: Allow your applications to consume [data streams](https://en.wikipedia.org/wiki/Data_stream) for which they cannot control the frequency using a push-based reactive approach. Handle cross-cutting concerns like logging, throttling, filtering etc. using configurable middlewares. Keep your applications scalable by moving stream consumers from a modular monolith to a distributed application with minimal friction.

Head over to our [reactive streaming recipes](#reactive-streaming-introduction) for more guidance on how to use this library.

[![NuGet version (Conqueror.Streaming.Reactive)](https://img.shields.io/nuget/v/Conqueror.Streaming.Reactive?label=Conqueror.Streaming.Reactive)](https://www.nuget.org/packages/Conqueror.Streaming.Reactive/)

## Quickstart

This quickstart guide will let you jump right into the code without lengthy explanations (for more guidance head over to our [recipes](#recipes)). By following this guide you'll add HTTP commands and queries to your ASP.NET Core application. You can also find the [source code](recipes/quickstart) here in the repository.

```sh
# add relevant CQS packages
dotnet add package Conqueror.CQS
dotnet add package Conqueror.CQS.Analyzers
dotnet add package Conqueror.CQS.Middleware.Logging
dotnet add package Conqueror.CQS.Transport.Http.Server.AspNetCore
```

```csharp
// add Conqueror CQS to your services
builder.Services
       .AddConquerorCQS()
       .AddConquerorCQSTypesFromExecutingAssembly()
       .AddConquerorCQSLoggingMiddlewares();

builder.Services.AddControllers().AddConquerorCQSHttpControllers();
builder.Services.FinalizeConquerorRegistrations();
```

In [IncrementCounterByCommand.cs](recipes/quickstart/IncrementCounterByCommand.cs) create a command that increments a named counter by a given amount (for demonstration purposes the counter is stored in an environment variable instead of a database).

```csharp
using Conqueror;

namespace Quickstart;

[HttpCommand(Version = "v1")]
public sealed record IncrementCounterByCommand(string CounterName, int IncrementBy);

public sealed record IncrementCounterByCommandResponse(int NewCounterValue);

public interface IIncrementCounterByCommandHandler
    : ICommandHandler<IncrementCounterByCommand, IncrementCounterByCommandResponse>
{
}

internal sealed class IncrementCounterByCommandHandler
    : IIncrementCounterByCommandHandler, IConfigureCommandPipeline
{
    // add logging to the command pipeline and configure the pre-execution log
    // level (only for demonstration purposes since the default is the same)
    public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
        pipeline.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Information);

    public async Task<IncrementCounterByCommandResponse> ExecuteCommand(IncrementCounterByCommand command,
                                                                        CancellationToken cancellationToken = default)
    {
        // simulate an asynchronous operation
        await Task.CompletedTask;

        var envVariableName = $"QUICKSTART_COUNTERS_{command.CounterName}";
        var counterValue = int.Parse(Environment.GetEnvironmentVariable(envVariableName) ?? "0");
        var newCounterValue = counterValue + command.IncrementBy;
        Environment.SetEnvironmentVariable(envVariableName, newCounterValue.ToString());
        return new(newCounterValue);
    }
}
```

In [GetCounterValueQuery.cs](recipes/quickstart/GetCounterValueQuery.cs) create a query that returns the value of a counter with the given name.

```csharp
using Conqueror;

namespace Quickstart;

[HttpQuery(Version = "v1")]
public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);

public interface IGetCounterValueQueryHandler
    : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>
{
}

internal sealed class GetCounterValueQueryHandler
    : IGetCounterValueQueryHandler, IConfigureQueryPipeline
{
    // add logging to the query pipeline and configure the pre-execution log
    // level (only for demonstration purposes since the default is the same)
    public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) =>
        pipeline.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Information);

    public async Task<GetCounterValueQueryResponse> ExecuteQuery(GetCounterValueQuery query,
                                                                 CancellationToken cancellationToken = default)
    {
        // simulate an asynchronous operation
        await Task.CompletedTask;

        var envVariableName = $"QUICKSTART_COUNTERS_{query.CounterName}";
        var counterValue = int.Parse(Environment.GetEnvironmentVariable(envVariableName) ?? "0");
        return new(counterValue);
    }
}
```

Now launch your app and you can call the command and query via HTTP.

```sh
curl http://localhost:5000/api/v1/commands/incrementCounterBy --data '{"counterName":"test","incrementBy":2}' -H 'Content-Type: application/json'
# prints {"newCounterValue":2}

curl http://localhost:5000/api/v1/queries/getCounterValue?counterName=test
# prints {"counterValue":2}
```

Thanks to the logging middleware we added to the command and query pipelines, you will see output similar to this in the server console.

```log
info: Quickstart.IncrementCounterByCommand[0]
      Executing command with payload {"CounterName":"test","IncrementBy":2} (Command ID: cb9a2563f1fc4965acf1a5f972532e81, Trace ID: fe675fdbf9a987620af31a474bf7ae8c)
info: Quickstart.IncrementCounterByCommand[0]
      Executed command and got response {"NewCounterValue":2} in 4.2150ms (Command ID: cb9a2563f1fc4965acf1a5f972532e81, Trace ID: fe675fdbf9a987620af31a474bf7ae8c)
info: Quickstart.GetCounterValueQuery[0]
      Executing query with payload {"CounterName":"test"} (Query ID: 802ddda5d6b14a68a725a27a1c0f7a1d, Trace ID: 8fdfa04f8c45ae3174044be0001a6e96)
info: Quickstart.GetCounterValueQuery[0]
      Executed query and got response {"CounterValue":2} in 2.9833ms (Query ID: 802ddda5d6b14a68a725a27a1c0f7a1d, Trace ID: 8fdfa04f8c45ae3174044be0001a6e96)
```

If you have swagger UI enabled, it will show the new command and query and they can be called from there.

![Quickstart Swagger](/recipes/quickstart/swagger.gif?raw=true "Quickstart Swagger")

## Recipes

In addition to code-level API documentation, **Conqueror** provides you with recipes that will guide you in how to utilize it to its maximum. Each recipe will help you solve one particular challenge that you will likely encounter while building a .NET application.

> For every "How do I do X?" you can imagine for this project, you should be able to find a recipe here. If you don't see a recipe for your question, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new) or even better, provide the recipe as a pull request.

### CQS Introduction

CQS is an acronym for [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation) (which is the inspiration for this project and also where the name is derived from: conquer -> **co**mmands a**n**d **quer**ies). The core idea behind this pattern is that operations which only read data (i.e. queries) and operations which mutate data or cause side-effects (i.e. commands) have very different characteristics (for a start, in most applications queries are executed much more frequently than commands). In addition, business operations often map very well to commands and queries, allowing you to model your application in a way that allows technical and business stakeholders alike to understand the capabilities of the system. There are many other benefits we gain from following this separation in our application logic. For example, commands and queries represent a natural boundary for encapsulation, provide clear contracts for modularization, and allow solving cross-cutting concerns according to the nature of the operation (e.g. caching makes sense for queries, but not such much for commands). With commands and queries testing often becomes more simple as well, since they provide a clear list of the capabilities that should be tested (allowing more focus to be placed on use-case-driven testing instead of traditional unit testing).

#### CQS Basics

- [getting started](recipes/cqs/basic/getting-started#readme)
- [testing command and query handlers](recipes/cqs/basic/testing-handlers#readme) _(to-be-written)_
- [solving cross-cutting concerns with middlewares (e.g. validation or logging)](recipes/cqs/basic/solving-cross-cutting-concerns#readme) _(to-be-written)_
- [testing command and query handlers that have middleware pipelines](recipes/cqs/basic/testing-handlers-with-pipelines#readme) _(to-be-written)_
- [testing middlewares](recipes/cqs/basic/testing-middlewares#readme) _(to-be-written)_

#### CQS Advanced

- [using a different dependency injection container (e.g. Autofac or Ninject)](recipes/cqs/advanced/different-dependency-injection#readme) _(to-be-written)_
- [creating a clean architecture with commands and queries](recipes/cqs/advanced/clean-architecture#readme) _(to-be-written)_
- [exposing commands and queries via HTTP](recipes/cqs/advanced/exposing-via-http#readme) _(to-be-written)_
- [testing HTTP commands and queries](recipes/cqs/advanced/testing-http#readme) _(to-be-written)_
- [calling HTTP commands and queries from another application](recipes/cqs/advanced/calling-http#readme) _(to-be-written)_
- [using middlewares for command and query HTTP clients](recipes/cqs/advanced/middlewares-for-http-clients#readme) _(to-be-written)_
- [authenticating and authorizing commands and queries](recipes/cqs/advanced/auth#readme) _(to-be-written)_
- [moving from a modular monolith to a distributed system](recipes/cqs/advanced/monolith-to-distributed#readme) _(to-be-written)_
- [using commands and queries in a Blazor app (server-side or web-assembly)](recipes/cqs/advanced/blazor-server#readme) _(to-be-written)_
- [building a CLI using commands and queries](recipes/cqs/advanced/building-cli#readme) _(to-be-written)_

#### CQS Expert

- [store and access background context information in the scope of a single command or query](recipes/cqs/expert/command-query-context#readme) _(to-be-written)_
- [propagate background context information (e.g. trace ID) across multiple commands, queries, events, and streams](recipes/cqs/expert/conqueror-context#readme) _(to-be-written)_
- [accessing properties of commands and queries in middlewares](recipes/cqs/expert/accessing-properties-in-middlewares#readme) _(to-be-written)_
- [exposing and calling commands and queries via other transports (e.g. gRPC)](recipes/cqs/expert/exposing-via-other-transports#readme) _(to-be-written)_
- [building test assertions that work for HTTP and non-HTTP commands and queries](recipes/cqs/expert/building-test-assertions-for-http-and-non-http#readme) _(to-be-written)_
- [creating generic command or query handlers](recipes/cqs/expert/generic-handlers#readme) _(to-be-written)_

### Eventing Introduction

Eventing is a way to refer to the publishing and observing of events via the [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) pattern. Eventing is a good way to decouple or loosely couple different parts of your application by making an event publisher agnostic to the observers of events it publishes. In addition to this basic idea, **Conqueror** allows solving cross-cutting concerns on both the publisher as well as the observer side.

#### Eventing Basics

- [getting started](recipes/eventing/basic/getting-started#readme) _(to-be-written)_
- [testing event observers](recipes/eventing/basic/testing-observers#readme) _(to-be-written)_
- [testing code that publishes events](recipes/eventing/basic/testing-publish#readme) _(to-be-written)_
- [solving cross-cutting concerns with middlewares (e.g. logging)](recipes/eventing/basic/solving-cross-cutting-concerns#readme) _(to-be-written)_
- [testing event observers with pipelines](recipes/eventing/basic/testing-observers-with-pipelines#readme) _(to-be-written)_
- [testing event publisher pipeline](recipes/eventing/basic/testing-publisher-pipeline#readme) _(to-be-written)_
- [testing middlewares](recipes/eventing/basic/testing-middlewares#readme) _(to-be-written)_

#### Eventing Advanced

- [using a different dependency injection container (e.g. Autofac or Ninject)](recipes/eventing/advanced/different-dependency-injection#readme) _(to-be-written)_
- [execute event observers with a different strategy (e.g. parallel execution)](recipes/eventing/advanced/publishing-strategy#readme) _(to-be-written)_
- [creating a clean architecture with loose coupling via events](recipes/eventing/advanced/clean-architecture#readme) _(to-be-written)_
- [moving from a modular monolith to a distributed system](recipes/eventing/advanced/monolith-to-distributed#readme) _(to-be-written)_

#### Eventing Expert

- [store and access background context information in the scope of a single event](recipes/eventing/expert/event-context#readme) _(to-be-written)_
- [propagate background context information (e.g. trace ID) across multiple commands, queries, events, and streams](recipes/eventing/expert/conqueror-context#readme) _(to-be-written)_
- [accessing properties of events in middlewares](recipes/eventing/expert/accessing-properties-in-middlewares#readme) _(to-be-written)_

### Interactive Streaming Introduction

For [data streaming](https://en.wikipedia.org/wiki/Data_stream) there are generally two high-level approaches: interactive / pull-based (i.e. consumer is in control of the pace) and reactive / push-based (i.e. the producer is in control of the pace). Here we focus on interactive streaming, which is a good approach for use cases like paging and event sourcing.

#### Interactive Streaming Basics

- [getting started](recipes/streaming.interactive/basic/getting-started#readme) _(to-be-written)_
- [testing streaming request handlers](recipes/streaming.interactive/basic/testing-handlers#readme) _(to-be-written)_
- [solving cross-cutting concerns with middlewares (e.g. validation or logging)](recipes/streaming.interactive/basic/solving-cross-cutting-concerns#readme) _(to-be-written)_
- [testing streaming request handlers that have middleware pipelines](recipes/streaming.interactive/basic/testing-handlers-with-pipelines#readme) _(to-be-written)_
- [testing middlewares](recipes/streaming.interactive/basic/testing-middlewares#readme) _(to-be-written)_

#### Interactive Streaming Advanced

- [using a different dependency injection container (e.g. Autofac or Ninject)](recipes/streaming.interactive/advanced/different-dependency-injection#readme) _(to-be-written)_
- [reading interactive streams from a messaging system (e.g. Kafka or RabbitMQ)](recipes/streaming.interactive/advanced/reading-from-messaging-system#readme) _(to-be-written)_
- [exposing streams via HTTP](recipes/streaming.interactive/advanced/exposing-via-http#readme) _(to-be-written)_
- [testing HTTP streams](recipes/streaming.interactive/advanced/testing-http#readme) _(to-be-written)_
- [consuming HTTP streams from another application](recipes/streaming.interactive/advanced/consuming-http#readme) _(to-be-written)_
- [using middlewares for interactive streaming HTTP clients](recipes/streaming.interactive/advanced/middlewares-for-http-clients#readme) _(to-be-written)_
- [optimize HTTP streaming performance with pre-fetching](recipes/streaming.interactive/advanced/optimize-http-performance#readme) _(to-be-written)_
- [authenticating and authorizing streaming requests](recipes/streaming.interactive/advanced/auth#readme) _(to-be-written)_
- [moving from a modular monolith to a distributed system](recipes/streaming.interactive/advanced/monolith-to-distributed#readme) _(to-be-written)_

#### Interactive Streaming Expert

- [store and access background context information in the scope of a single streaming request](recipes/streaming.interactive/expert/streaming-request-context#readme) _(to-be-written)_
- [propagate background context information (e.g. trace ID) across multiple commands, queries, events, and streams](recipes/streaming.interactive/expert/conqueror-context#readme) _(to-be-written)_
- [accessing properties of streaming requests in middlewares](recipes/streaming.interactive/expert/accessing-properties-in-middlewares#readme) _(to-be-written)_
- [exposing and consuming interactive streams via other transports (e.g. SignalR)](recipes/streaming.interactive/expert/exposing-via-other-transports#readme) _(to-be-written)_
- [building test assertions that work for HTTP and non-HTTP streams](recipes/streaming.interactive/expert/building-test-assertions-for-http-and-non-http#readme) _(to-be-written)_

### Reactive Streaming Introduction

For [data streaming](https://en.wikipedia.org/wiki/Data_stream) there are generally two high-level approaches: interactive / pull-based (i.e. consumer is in control of the pace) and reactive / push-based (i.e. the producer is in control of the pace). Here we focus on reactive streaming, which is a good approach when you do not control the source of the stream yourself, and therefore need to handle stream items at whatever pace the producer provides (e.g. handling sensor data from IoT devices).

#### Reactive Streaming Basics

- [tbd](recipes/streaming.reactive/basic/tbd#readme) _(to-be-written)_

#### Reactive Streaming Advanced

- [tbd](recipes/streaming.reactive/advanced/tbd#readme) _(to-be-written)_

#### Reactive Streaming Expert

- [tbd](recipes/streaming.reactive/expert/tbd#readme) _(to-be-written)_

## Motivation

Modern software development is often centered around building web applications that communicate via [HTTP](https://en.wikipedia.org/wiki/Hypertext_Transfer_Protocol) (we'll call them "web APIs"). However, many applications require different entry points or APIs as well (e.g. message queues, command line interfaces, raw TCP or UDP sockets, etc.). Each of these kinds of APIs need to address a variety of cross-cutting concerns, most of which apply to all kinds of APIs (e.g. logging, tracing, error handling, authorization, etc.). Microsoft has done an excellent job in providing out-of-the-box solutions for many of these concerns when building web APIs with [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core) using [middlewares](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-7.0) (which implement the [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) pattern). However, for other kinds of APIs, development teams are often forced to handle these concerns themselves, spending valuable development time.

One way many teams choose to address this issue is by forcing every operation to go through a web API (e.g. having a small adapter that reads messages from a queue and then calls a web API for processing the message). While this works well in many cases, it adds extra complexity and fragility by adding a new integration point for very little value. Optimally, there would be a way to address the cross-cutting concerns in a consistent way for all kinds of APIs. This is exactly what **Conqueror** does. It provides the building blocks for implementing business functionality and addressing those cruss-cutting concerns in an transport-agnostic fashion, and provides extension packages that allow exposing the business functionality via different transports (e.g. HTTP).

In addition, a popular way to build systems these days is using [microservices](https://microservices.io). While microservices are a powerful approach, they can often represent a significant challenge for small or new teams, mostly for deployment and operations (challenges common to most [distributed systems](https://en.wikipedia.org/wiki/Distributed_computing)). A different approach that many teams choose is to start with a [modular monolith](https://martinfowler.com/bliki/MonolithFirst.html) and move to microservices at a later point. However, it is common for teams to struggle with such a migration, partly due to sub-optimal modularization and partly due to existing tools and libraries not providing a smooth transition journey from one approach to another (or often forcing you into the distributed approach directly, e.g. [MassTransit](https://masstransit-project.com)). **Conqueror** addresses this by encouraging you to build modules with clearly defined contracts and by allowing you to switch from having a module be part of a monolith to be its own microservice with minimal code changes.

In summary, these are some of the strengths of **Conqueror**:

- **Providing building blocks for many different communication patterns:** Many applications require the use of different communication patterns to fulfill their business requirements (e.g. `request-response`, `fire-and-forget`, `publish-subscribe`, `streaming` etc.). **Conqueror** provides building blocks for implementing these communication patterns efficiently and consistently, while allowing you to address cross-cutting concerns in a transport-agnostic fashion.

- **Excellent use-case-driven documentation:** A lot of effort went into writing our [recipes](#recipes). While most other libraries have documentation that is centered around explaining _what_ they do, our use-case-driven documentation is focused on showing you how **Conqueror** _helps you to solve the concrete challenges_ your are likely to encounter during application development.

- **Strong focus on testability:** Testing is a very important topic that is sadly often neglected. **Conqueror** takes testability very seriously and makes sure that you know how you can test the code you have written using it (you may have noticed that the **Conqueror.CQS** recipe immediately following [getting started](recipes/cqs/basic/getting-started#readme) shows you how you can [test the handlers](recipes/cqs/basic/testing-handlers#readme) we built in the first recipe).

<!---
- **Out-of-the-box solutions for many common yet often complex use-cases:** Many development teams spend valuable time on solving common cross-cutting concerns like validation, logging, error handling etc. over and over again. **Conqueror** provides a variety of pre-built middlewares that help you address those concerns with minimal effort.
-->

- **Migrating from a modular monolith to a distributed system with minimal friction:** Business logic built on top of **Conqueror** provides clear contracts to consumers, regardless of whether these consumers are located in the same process or in a different application. By abstracting away the concrete transport over which the business logic is called, it can easily be moved from a monolithic approach to a distributed approach with minimal code changes.

- **Modular and extensible architecture:** Instead of a big single library, **Conqueror** consists of many small (independent or complementary) packages. This allows you to pick and choose what functionality you want to use without adding the extra complexity for anything that you don't. It also improves maintainability by allowing modifications and extensions with a lower risk of breaking any existing functionality (in addition to a high level of public-API-focused test coverage).

### Comparison with similar projects

Below you can find a brief comparison with some popular projects which address similar concerns as **Conqueror**.

#### Differences to MediatR

The excellent library [MediatR](https://github.com/jbogard/MediatR) is a popular choice for building applications. **Conqueror** takes a lot of inspirations from its design, with some key differences:

- MediatR allows handling cross-cutting concerns with global behaviors, while **Conqueror** allows handling these concerns with composable middlewares in independent pipelines per handler type.
- MediatR uses a single message sender service which makes it tricky to navigate to a message handler in your IDE from the point where the message is sent. With **Conqueror** you call handlers through an explicit interface, allowing you to use the "Go to implementation" functionality of your IDE.
- MediatR is focused building single applications without any support for any transports, while **Conqueror** allows building both single applications as well as distributed systems that communicate via different transports implemented through adapters.

#### Differences to MassTransit

[MassTransit](https://masstransit-project.com) is a great framework for building distributed applications. It addresses many of the same concerns as **Conqueror**, with some key differences:

- MassTransit is designed for building distributed systems, forcing you into this approach from the start, even if you don't need it yet (the provided in-memory transport is explicitly mentioned as not being suited for production usage). **Conqueror** allows building both single applications as well as distributed systems.
- MassTransit is focused on asynchronous messaging, while **Conqueror** provides more communication patterns (e.g. synchronous request-response over HTTP).
- MassTransit has adapters for many messaging middlewares, like RabbitMQ or Azure Service Bus, which **Conqueror** does not.
- MassTransit provides out-of-the-box solutions for advanced patterns like sagas, state machines, etc., which **Conqueror** does not.

If you require the advanced patterns or messaging middleware connectors which MassTransit provides, you can easily combine it with **Conqueror** by calling command and query handlers from your consumers or wrapping your producers in command handlers.
