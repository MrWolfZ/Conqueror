# Core architecture findings

## Scope and files read

- `src/core/Conqueror.Abstractions/Messaging/IMessage.cs`
- `src/core/Conqueror.Abstractions/Messaging/IMessageHandler.cs`
- `src/core/Conqueror.Abstractions/Messaging/IMessagePipeline.cs`
- `src/core/Conqueror.Abstractions/Messaging/MessageMiddlewareContext.cs`
- `src/core/Conqueror.Abstractions/Signalling/ISignal.cs`
- `src/core/Conqueror.Abstractions/Iterating/IIterator.cs`
- `src/core/Conqueror.Abstractions/Context/ConquerorContext.cs`
- `src/core/Conqueror/Context/DefaultConquerorContext.cs`
- `src/core/Conqueror/Context/DefaultConquerorContextAccessor.cs`
- `src/core/Conqueror/Messaging/MessageDispatcher.cs`
- `src/core/Conqueror/Messaging/MessagePipeline.cs`
- `src/core/Conqueror/Messaging/MessageHandlerRegistry.cs`
- `src/core/Conqueror/Signalling/SignalDispatcher.cs`
- `src/core/Conqueror/Iterating/IteratorDispatcher.cs`
- `src/core/Conqueror/*ServiceCollectionExtensions.cs`

## What the core layer does

Conqueror is a strongly typed runtime for three communication patterns:

- messages: request/response
- signals: publish/subscribe
- iterators: request/stream

Each pattern has a type abstraction, handler abstraction, pipeline abstraction, dispatcher, registry, and transport boundary. The core strategy is compile-time metadata plus runtime DI and pipeline execution, with reflection minimized for AOT compatibility.

## Main abstractions and flow

Messages are defined by `IMessage<TMessage, TResponse>`, whose generated implementation supplies `CoreTypesInjector`, `EmptyInstance`, `JsonSerializerContext`, public constructor/property metadata, and a static handler invocation method. The generated nested `IHandler` and `IPipeline` types give users the ergonomic API while tying message, response, handler, proxy, and pipeline types together.

`MessageHandlerProxy<TMessage, TResponse, TIHandler>` is the fluent facade users actually call. It exposes `WithPipeline(...)`, `WithTransport(...)`, and `Handle(...)`, then delegates to `MessageDispatcher`. Signal and iterator proxies follow the same shape, with signals returning `Task` and iterators returning `IAsyncEnumerable<TItem>`.

`MessageDispatcher`, `SignalDispatcher`, and `IteratorDispatcher` clone or create ambient context, assign operation IDs, choose the default in-process transport if no transport was configured, and execute the configured pipeline.

Pipelines are ordered middleware lists. `MessagePipeline<TMessage,TResponse>.Execute(...)` creates a `MessageMiddlewareContext<TMessage,TResponse>` and starts at the first middleware. `ctx.Next(...)` either advances to the next middleware or invokes the sender. Signal and iterator contexts mirror this pattern.

`DefaultConquerorContextAccessor` stores the current context in `AsyncLocal`. `DefaultConquerorContext` supports child contexts, upstream propagation on dispose, transportable string key/value data, in-process object data, trace IDs, operation IDs, and `ClaimsPrincipal`.

Registries store generated handler registrations and map message/signal/iterator types plus type-injector types to invokers. This enables transports to ask for handlers without late reflection.

## Design patterns extracted

- Source-generated static metadata for message/signal/iterator types.
- Self-referential generics / F-bounded polymorphism for handler identity.
- Fluent proxy objects to hide dispatch, pipeline, and transport plumbing.
- Chain-of-responsibility middleware with explicit `ctx.Next(...)`.
- Ambient async execution context with parent/child copy and upstream propagation.
- Transport abstraction with in-process default and call-site transport selection.
- Type-injector pattern for AOT-friendly registration and transport metadata.
- DI-centered runtime composition with generated module-initializer registration.

## C#/.NET-specific dependencies

- Static abstract and static virtual interface members.
- Roslyn source generators and generated nested partial interfaces/classes.
- `IServiceCollection`, `IServiceProvider`, DI lifetimes.
- `AsyncLocal`, `Activity.Current`, `ClaimsPrincipal`.
- `Task`, `IAsyncEnumerable<T>`, `CancellationToken`.
- `System.Text.Json.Serialization.JsonSerializerContext`.
- `[ModuleInitializer]` and trimming/AOT annotations.

## Rust portability assessment

The concepts are portable. A Rust version could model messages, signals, streams, middleware, transports, context, and handler registries with traits, generics, procedural macros, `serde`, `tower`, `tokio`, and `Stream`.

The exact C# API is not portable. Rust lacks nested generated interfaces, static abstract interface members, module initializers in the .NET sense, and a standard DI container. The natural Rust design would use derives/attribute macros, associated types/constants, explicit registration, `Arc`-backed shared state, `tower::Service`/`Layer`, and either boxed futures or `async-trait` for handler object safety.

## Open questions / risks

- Whether Rust should preserve implicit discovery or intentionally require explicit registration.
- Whether ambient context should use task-local storage or be passed explicitly through request context.
- How much dynamic dispatch is acceptable for ergonomic call-site APIs.
- Whether iterator streaming semantics should match the .NET protocols exactly or be redesigned around idiomatic Rust streams.
