# Conqueror - for building scalable & maintainable .NET applications

**Conqueror** is a set of libraries that helps you build .NET applications in a structured way (using patterns like [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation), [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) (often also known as middlewares), [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern), [data streams](https://en.wikipedia.org/wiki/Data_stream), etc.), while keeping them scalable (both at development time as well as runtime).

See our [quickstart](quickstart) or [example projects](examples) if you want to jump right into using **Conqueror** by code examples. Or head over to our [recipes](#recipes) for more detailed guidance on how you can utilize **Conqueror** to its maximum. Finally, if you want to learn more about the motivation behind this project (including comparisons to similar projects like [MediatR](https://github.com/jbogard/MediatR)), head over to the [motivation](#motivation) section.

[![Build Status](https://github.com/MrWolfZ/Conqueror/actions/workflows/dotnet.yml/badge.svg)](https://github.com/MrWolfZ/Conqueror/actions/workflows/dotnet.yml)
[![license](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Libraries

**Conqueror.CQS (_stable_)**: Split your business processes into simple-to-maintain and easy-to-test pieces of code using the [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation) pattern. Handle cross-cutting concerns like logging, validation, authorization etc. using configurable middlewares. Keep your applications scalable by moving commands and queries from a modular monolith to a distributed application with minimal friction.

[![NuGet version (Conqueror.CQS)](https://img.shields.io/nuget/v/Conqueror.CQS?label=Conqueror.CQS)](https://www.nuget.org/packages/Conqueror.CQS/)

**Conqueror.Eventing (_experimental_)**: Decouple your application logic by using in-process event publishing using the [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) pattern. Handle cross-cutting concerns like logging, tracing, filtering etc. using configurable middlewares.

[![NuGet version (Conqueror.Eventing)](https://img.shields.io/nuget/v/Conqueror.Eventing?label=Conqueror.Eventing)](https://www.nuget.org/packages/Conqueror.Eventing/)

**Conqueror.Streaming.Interactive (_experimental_)**: Keep your applications in control by allowing them to consume [data streams](https://en.wikipedia.org/wiki/Data_stream) at their own pace using a pull-based interactive approach. Handle cross-cutting concerns like logging, error handling, authorization etc. using configurable middlewares. Keep your applications scalable by moving stream consumers from a modular monolith to a distributed application with minimal friction.

[![NuGet version (Conqueror.Streaming.Interactive)](https://img.shields.io/nuget/v/Conqueror.Streaming.Interactive?label=Conqueror.Streaming.Interactive)](https://www.nuget.org/packages/Conqueror.Streaming.Interactive/)

**Conqueror.Streaming.Reactive (_early prototype_)**: Allow your applications to consume [data streams](https://en.wikipedia.org/wiki/Data_stream) for which they cannot control the frequency using a push-based reactive approach. Handle cross-cutting concerns like logging, throttling, filtering etc. using configurable middlewares. Keep your applications scalable by moving stream consumers from a modular monolith to a distributed application with minimal friction.

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

- [accessing properties of commands and queries in middlewares](recipes/cqs/expert/accessing-properties-in-middlewares#readme) _(to-be-written)_
- [exposing and calling commands and queries via other transports (e.g. gRPC)](recipes/cqs/expert/exposing-via-other-transports#readme) _(to-be-written)_
- [building test assertions that work for HTTP and non-HTTP commands and queries](recipes/cqs/expert/building-test-assertions-for-http-and-non-http#readme) _(to-be-written)_

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
