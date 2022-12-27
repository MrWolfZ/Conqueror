# Conqueror - for building scalable & maintainable .NET applications

**Conqueror** is a set of libraries that helps you build .NET applications in a structured way (using patterns like [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation), [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) (often also known as middlewares), [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern), [data streams](https://en.wikipedia.org/wiki/Data_stream), etc.), while keeping them scalable (both at development time as well as runtime).

See our [quickstart](quickstart) or [example projects](examples) if you want to jump right into using **Conqueror** by code examples. Or head over to our [recipes](#recipes) for more detailed guidance on how you can utilize **Conqueror** to its maximum. Finally, if you want to learn more about the motivation behind this project (including comparisons to similar projects like [MediatR](https://github.com/jbogard/MediatR)), head over to the [motivation](#motivation) section.

[![Build Status](https://github.com/MrWolfZ/Conqueror/actions/workflows/dotnet.yml/badge.svg)](https://github.com/MrWolfZ/Conqueror/actions/workflows/dotnet.yml)
[![license](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Libraries

**Conqueror.CQS (_stable_)**: Split your business processes into simple-to-maintain and easy-to-test pieces of code using the [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation) pattern. Handle cross-cutting concerns like logging, validation, authorization etc. using configurable middlewares. Keep your applications scalable by moving commands and queries from a modular monolith to multiple apps with minimal friction.

[![NuGet version (Conqueror.CQS)](https://img.shields.io/nuget/v/Conqueror.CQS?label=Conqueror.CQS)](https://www.nuget.org/packages/Conqueror.CQS/)

**Conqueror.Eventing (_experimental_)**: Decouple your application logic by using in-process event publishing using the [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) pattern. Handle cross-cutting concerns like logging, tracing, filtering etc. using configurable middlewares.

[![NuGet version (Conqueror.Eventing)](https://img.shields.io/nuget/v/Conqueror.Eventing?label=Conqueror.Eventing)](https://www.nuget.org/packages/Conqueror.Eventing/)

**Conqueror.Streaming.Interactive (_experimental_)**: Keep your applications in control by allowing them to consume [data streams](https://en.wikipedia.org/wiki/Data_stream) at their own pace using a pull-based interactive approach. Handle cross-cutting concerns like logging, error handling, authorization etc. using configurable middlewares. Keep your applications scalable by moving stream consumers from a modular monolith to multiple apps with minimal friction.

[![NuGet version (Conqueror.Streaming.Interactive)](https://img.shields.io/nuget/v/Conqueror.Streaming.Interactive?label=Conqueror.Streaming.Interactive)](https://www.nuget.org/packages/Conqueror.Streaming.Interactive/)

**Conqueror.Streaming.Reactive (_early prototype_)**: Allow your applications to consume [data streams](https://en.wikipedia.org/wiki/Data_stream) for which they cannot control the frequency using a push-based reactive approach. Handle cross-cutting concerns like logging, throttling, filtering etc. using configurable middlewares. Keep your applications scalable by moving stream consumers from a modular monolith to multiple apps with minimal friction.

[![NuGet version (Conqueror.Streaming.Reactive)](https://img.shields.io/nuget/v/Conqueror.Streaming.Reactive?label=Conqueror.Streaming.Reactive)](https://www.nuget.org/packages/Conqueror.Streaming.Reactive/)

## Quickstart

TODO

## Recipes

Instead of traditional documentation **Conqueror** has these recipes that will show you how you can utilize it to its maximum. Each recipe will help you solve one particular challenge that you will likely encounter while building a .NET application.

> For every "How do I do X?" you can imagine for this project, you should be able to find a recipe here. If you don't see a recipe for your question, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new) or even better, provide the recipe as a pull request.

### Basics

TODO

### Advanced

TODO

### Expert

TODO

## Motivation

TODO

### Design philosophy

TODO

### Comparison with similar projects

TODO
