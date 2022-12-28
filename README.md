# Conqueror - for building scalable & maintainable .NET applications

> ATTENTION: This project is currently still undergoing active development and contrary to what some of this README says, everything in here is still subject to change. Therefore please do not yet use this project for any production application.

**Conqueror** is a set of libraries that helps you build .NET applications in a structured way (using patterns like [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation), [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) (often also known as middlewares), [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern), [data streams](https://en.wikipedia.org/wiki/Data_stream), etc.), while keeping them scalable (both from the development perspective as well as at runtime).

See our [quickstart](quickstart) or [example projects](examples) if you want to jump right into code examples for using **Conqueror**. Or head over to our [recipes](#recipes) for more detailed guidance on how you can utilize **Conqueror** to its maximum. Finally, if you want to learn more about the motivation behind this project (including comparisons to similar projects like [MediatR](https://github.com/jbogard/MediatR)), head over to the [motivation](#motivation) section.

[![Build Status](https://github.com/MrWolfZ/Conqueror/actions/workflows/dotnet.yml/badge.svg)](https://github.com/MrWolfZ/Conqueror/actions/workflows/dotnet.yml)
[![license](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

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

TODO

## Recipes

Instead of traditional documentation **Conqueror** has these recipes that will show you how you can utilize it to its maximum. Each recipe will help you solve one particular challenge that you will likely encounter while building a .NET application.

> For every "How do I do X?" you can imagine for this project, you should be able to find a recipe here. If you don't see a recipe for your question, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new) or even better, provide the recipe as a pull request.

### CQS Introduction

CQS is an acronym for [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation). For a full exploration of why we chose this pattern as the foundation for one of the **Conqueror** libraries see the [motivation](#motivation) section. Here is a short explanation: the core idea behind this pattern is that operations which only read data (i.e. queries) and operations which mutate data or cause side-effects (i.e. commands) have very different characteristics (for a start, in most applications queries are executed much more frequently than commands). In addition, business operations often map very well to commands and queries. By following this separation in our application logic, we gain many benefits. For example, commands and queries represent a natural boundary for encapsulation and cross-cutting concerns can be solved generally according to the nature of the operation (e.g. caching makes sense for queries, but not such much for commands).

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

Interactive streaming is ...

#### Interactive Streaming Basics

- [tbd](recipes/streaming.interactive/basic/tbd#readme) _(to-be-written)_

#### Interactive Streaming Advanced

- [tbd](recipes/streaming.interactive/advanced/tbd#readme) _(to-be-written)_

#### Interactive Streaming Expert

- [tbd](recipes/streaming.interactive/expert/tbd#readme) _(to-be-written)_

### Reactive Streaming Introduction

Reactive streaming is ...

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
