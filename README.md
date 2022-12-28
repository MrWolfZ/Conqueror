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
# add server-side CQS packages
dotnet add package Conqueror.CQS
dotnet add package Conqueror.CQS.Transport.Http.Server.AspNetCore
```

```csharp
// add Conqueror CQS to your services
builder.Services.AddConquerorCQS().AddConquerorCQSTypesFromExecutingAssembly();
builder.Services.AddControllers().AddConquerorCQSHttpControllers();
builder.Services.FinalizeConquerorRegistrations();
```

In `PrintIntegerCommand.cs` create a command that prints its parameter to stdout and echos it back to the client.

```csharp
using Conqueror;

namespace Quickstart;

[HttpCommand]
public sealed record PrintIntegerCommand(int Parameter);

public sealed record PrintIntegerCommandResponse(int Parameter);

public interface IPrintIntegerCommandHandler : ICommandHandler<PrintIntegerCommand,
                                                               PrintIntegerCommandResponse>
{
}

public sealed class PrintIntegerCommandHandler : IPrintIntegerCommandHandler
{
    public Task<PrintIntegerCommandResponse> ExecuteCommand(PrintIntegerCommand command,
                                                            CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Got command parameter {command.Parameter}");
        return Task.FromResult(new PrintIntegerCommandResponse(command.Parameter));
    }
}
```

In `AddTwoIntegersQuery.cs` create a query that takes two integer parameters and returns their sum.

```csharp
using Conqueror;

namespace Quickstart;

[HttpQuery]
public sealed record AddTwoIntegersQuery(int Parameter1, int Parameter2);

public sealed record AddTwoIntegersQueryResponse(int Sum);

public interface IAddTwoIntegersQueryHandler : IQueryHandler<AddTwoIntegersQuery,
                                                             AddTwoIntegersQueryResponse>
{
}

public sealed class AddTwoIntegersQueryHandler : IAddTwoIntegersQueryHandler
{
    public Task<AddTwoIntegersQueryResponse> ExecuteQuery(AddTwoIntegersQuery query,
                                                          CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AddTwoIntegersQueryResponse(query.Parameter1 + query.Parameter2));
    }
}
```

Now launch your app and you can call the command and query via HTTP.

```sh
curl http://localhost:5000/api/commands/printInteger --data '{"parameter": 10}' -H 'Content-Type: application/json'
# in your server console you will see "Got command parameter 10"

curl http://localhost:5000/api/queries/addTwoIntegers?parameter1=10\&parameter2=5 
# prints {"sum":15}
```

If you have swagger UI enabled, it will show the new command and query and they can be called from there.

![Quickstart Swagger](/recipes/quickstart/swagger.gif?raw=true "Quickstart Swagger")

## Recipes

In addition to code-level API documentation, **Conqueror** provides you with recipes that will guide you in how to utilize it to its maximum. Each recipe will help you solve one particular challenge that you will likely encounter while building a .NET application.

> For every "How do I do X?" you can imagine for this project, you should be able to find a recipe here. If you don't see a recipe for your question, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new) or even better, provide the recipe as a pull request.

### CQS Introduction

CQS is an acronym for [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation) (which is the inspiration for this project and also where the name is derived from: conquer -> **co**mmands a**n**d **quer**ies). For a full exploration of why we chose this pattern as the foundation for one of the **Conqueror** libraries see the [motivation](#motivation) section. Here is a short explanation: the core idea behind this pattern is that operations which only read data (i.e. queries) and operations which mutate data or cause side-effects (i.e. commands) have very different characteristics (for a start, in most applications queries are executed much more frequently than commands). In addition, business operations often map very well to commands and queries. By following this separation in our application logic, we gain many benefits. For example, commands and queries represent a natural boundary for encapsulation and cross-cutting concerns can be solved generally according to the nature of the operation (e.g. caching makes sense for queries, but not such much for commands).

#### CQS Basics

- [getting started](recipes/cqs/basic/getting-started#readme) _(to-be-written)_
- [testing command and query handlers](recipes/cqs/basic/testing-handlers#readme) _(to-be-written)_
- [reducing code repetition with custom handler interfaces](recipes/cqs/basic/reducing-code-repetition#readme) _(to-be-written)_

#### CQS Advanced

- [solving cross-cutting concerns with middlewares (e.g. logging)](recipes/cqs/advanced/solving-cross-cutting-concerns#readme) _(to-be-written)_
- [testing command and query handlers that have middleware pipelines](recipes/cqs/advanced/testing-handlers-with-pipelines#readme) _(to-be-written)_
- [testing middlewares](recipes/cqs/advanced/testing-middlewares#readme) _(to-be-written)_
- [making middleware pipelines reusable](recipes/cqs/advanced/making-pipelines-reusable#readme) _(to-be-written)_
- [validating commands and queries](recipes/cqs/advanced/validation#readme) _(to-be-written)_
- [creating a clean architecture with commands and queries](recipes/cqs/advanced/clean-architecture#readme) _(to-be-written)_
- [exposing commands and queries via HTTP](recipes/cqs/advanced/exposing-via-http#readme) _(to-be-written)_
- [testing HTTP commands and queries](recipes/cqs/advanced/testing-http#readme) _(to-be-written)_
- [calling HTTP commands and queries from another application](recipes/cqs/advanced/calling-http#readme) _(to-be-written)_
- [using middlewares for command and query HTTP clients](recipes/cqs/advanced/middlewares-for-http-clients#readme) _(to-be-written)_
- [authenticating and authorizing commands and queries](recipes/cqs/advanced/auth#readme) _(to-be-written)_

#### CQS Expert

- [store and access background context information in the scope of a single command or query](recipes/cqs/expert/command-query-context#readme) _(to-be-written)_
- [propagate background context information (e.g. trace ID) across multiple commands, queries, events, and streams](recipes/cqs/expert/conqueror-context#readme) _(to-be-written)_
- [accessing properties of commands and queries in middlewares](recipes/cqs/expert/accessing-properties-in-middlewares#readme) _(to-be-written)_
- [exposing and calling commands and queries via other transports (e.g. gRPC)](recipes/cqs/expert/exposing-via-other-transports#readme) _(to-be-written)_
- [building test assertions that work for HTTP and non-HTTP commands and queries](recipes/cqs/expert/building-test-assertions-for-http-and-non-http#readme) _(to-be-written)_

### Eventing Introduction

Eventing is a way to refer to the publishing and observing of events via the [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) pattern. Eventing is a good way to decouple or loosely couple different parts of your application by making an event publisher agnostic to the observers of events it publishes. In addition to this basic idea, **Conqueror** allows solving cross-cutting concerns on both the publisher as well as the observer side.

#### Eventing Basics

- [getting started](recipes/eventing/basic/getting-started#readme) _(to-be-written)_
- [testing event observers](recipes/eventing/basic/testing-observers#readme) _(to-be-written)_
- [testing code that publishes events](recipes/eventing/basic/testing-publish#readme) _(to-be-written)_
- [reducing code repetition with custom observer interfaces](recipes/eventing/basic/reducing-code-repetition#readme) _(to-be-written)_

#### Eventing Advanced

- [execute event observers with a different strategy (e.g. parallel execution)](recipes/eventing/advanced/publishing-strategy#readme) _(to-be-written)_
- [solving cross-cutting concerns with middlewares (e.g. logging)](recipes/eventing/advanced/solving-cross-cutting-concerns#readme) _(to-be-written)_
- [testing event observers with pipelines](recipes/eventing/advanced/testing-observers-with-pipelines#readme) _(to-be-written)_
- [testing event publisher pipeline](recipes/eventing/advanced/testing-publisher-pipeline#readme) _(to-be-written)_
- [testing middlewares](recipes/eventing/advanced/testing-middlewares#readme) _(to-be-written)_
- [making observer middleware pipelines reusable](recipes/eventing/advanced/making-observer-pipelines-reusable#readme) _(to-be-written)_
- [creating a clean architecture with loose coupling via events](recipes/eventing/advanced/clean-architecture#readme) _(to-be-written)_

#### Eventing Expert

- [store and access background context information in the scope of a single event](recipes/eventing/expert/event-context#readme) _(to-be-written)_
- [propagate background context information (e.g. trace ID) across multiple commands, queries, events, and streams](recipes/eventing/expert/conqueror-context#readme) _(to-be-written)_
- [accessing properties of events in middlewares](recipes/eventing/expert/accessing-properties-in-middlewares#readme) _(to-be-written)_

### Interactive Streaming Introduction

For [data streaming](https://en.wikipedia.org/wiki/Data_stream) there are generally two high-level approaches: interactive / pull-based (i.e. consumer is in control of the pace) and reactive / push-based (i.e. the producer is in control of the pace). Here we focus on interactive streaming, which is a good approach for use cases like paging and event sourcing.

#### Interactive Streaming Basics

- [getting started](recipes/streaming.interactive/basic/getting-started#readme) _(to-be-written)_
- [testing streaming request handlers](recipes/streaming.interactive/basic/testing-handlers#readme) _(to-be-written)_
- [reducing code repetition with custom handler interfaces](recipes/streaming.interactive/basic/reducing-code-repetition#readme) _(to-be-written)_

#### Interactive Streaming Advanced

- [solving cross-cutting concerns with middlewares (e.g. logging)](recipes/streaming.interactive/advanced/solving-cross-cutting-concerns#readme) _(to-be-written)_
- [testing streaming request handlers that have middleware pipelines](recipes/streaming.interactive/advanced/testing-handlers-with-pipelines#readme) _(to-be-written)_
- [testing middlewares](recipes/streaming.interactive/advanced/testing-middlewares#readme) _(to-be-written)_
- [making middleware pipelines reusable](recipes/streaming.interactive/advanced/making-pipelines-reusable#readme) _(to-be-written)_
- [validating streaming requests](recipes/streaming.interactive/advanced/validation#readme) _(to-be-written)_
- [exposing streams via HTTP](recipes/streaming.interactive/advanced/exposing-via-http#readme) _(to-be-written)_
- [testing HTTP streams](recipes/streaming.interactive/advanced/testing-http#readme) _(to-be-written)_
- [consuming HTTP streams from another application](recipes/streaming.interactive/advanced/consuming-http#readme) _(to-be-written)_
- [using middlewares for interactive streaming HTTP clients](recipes/streaming.interactive/advanced/middlewares-for-http-clients#readme) _(to-be-written)_
- [optimize HTTP streaming performance with pre-fetching](recipes/streaming.interactive/advanced/optimize-http-performance#readme) _(to-be-written)_
- [authenticating and authorizing streaming requests](recipes/streaming.interactive/advanced/auth#readme) _(to-be-written)_

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

- escaping the HTTP-centric world
- communication patterns in apps
  - request-response
  - fire-and-forget
  - publish-subscribe
  - reading data streams
    - interactive vs reactive
- what is CQS and why is it useful for modeling rich APIs

### Design philosophy

- providing a library that scales
  - many libraries either stay very basic or they require big upfront investment to scale
  - this lib provides a smooth journey for going from a prototype to a distributed application
- design for testability

### Comparison with similar projects

- differences to MediatR
- differences to MassTransit
