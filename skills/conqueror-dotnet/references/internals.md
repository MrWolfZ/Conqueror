# Conqueror internals for debugging app integration

Use this reference when app code fails because generated APIs, registration, context, pipelines, or transports are misunderstood.

## Source generation

Conqueror relies on source generation to avoid runtime reflection and support AOT-friendly patterns.

For a message:

```csharp
[Message<Response>]
public sealed partial record DoWork(string Value);
```

The generator creates:

- `DoWork.T`
- `DoWork.IHandler`
- `DoWork.IPipeline`
- metadata used by core and transports
- handler type injectors when a handler implements `DoWork.IHandler`
- module-initializer registration hooks for handler types

Signals get `.T` and nested `IHandler`. Messages and iterators get nested `IPipeline`. Transport attributes can add transport-specific generated interfaces and metadata.

Common generator-related failures:

- operation or handler type is not `partial`
- missing response/item type, especially `[Iterator<TItem>]`
- inconsistent response/item types across core and transport attributes
- handler implements the wrong interface
- code tries to hand-write generated members
- project does not reference the package that carries the relevant attributes/generator support

## Registration model

Assembly registration helpers use generated handler registration data. They are not broad reflection scans in the usual sense.

Typical app registration:

```csharp
services.AddMessageHandlersFromAssembly(typeof(Program).Assembly);
services.AddSignalHandlersFromAssembly(typeof(Program).Assembly);
```

If a handler is not found:

1. Confirm the handler is compiled into the assembly being registered.
2. Confirm the handler type is `partial`.
3. Confirm it implements the generated nested interface.
4. Confirm the correct registration helper is used for the operation model.
5. Confirm package references/source generation are active.

## Runtime dispatch

For messages:

1. `IMessageSenders.For(CreateTodo.T)` creates a generated proxy.
2. `.Handle(...)` calls the dispatcher.
3. Dispatcher clones or creates ambient context and assigns message IDs.
4. If no transport is configured, it uses in-process.
5. Caller-side pipeline runs.
6. Sender/transport runs.
7. In-process sender invokes the receiver-side handler invoker.
8. Handler-side pipeline runs.
9. Handler method runs.

Signals are similar but publish to zero, one, or many handlers through a broadcasting strategy. Sequential in-process broadcasting is the default. Parallel/custom strategies can be configured.

Iterators return `IAsyncEnumerable<TItem>`. Context has to survive across yield points, so iterator context propagation is more subtle than message/signal propagation.

## Transport roles

Middleware can observe different roles:

- message sender vs receiver
- signal publisher vs receiver
- iterator client vs server

This is why caller-side and handler-side logs may both appear for one in-process operation.

## Context behavior

Conqueror context is ambient and AsyncLocal-backed.

- `GetOrCreate()` reuses current context.
- Dispatchers clone context for child operations.
- Downstream/bidirectional data flows into child context.
- Upstream/bidirectional data flows back when the child context is disposed.
- Root trace IDs come from `Activity.Current` when available, otherwise generated.

Use this to reason about nested sends/publishes and loop-prevention flags.

## Practical debugging checklist

- Generated member missing? Check `partial`, attribute package, and source generator/build output.
- `For(X.T)` unavailable? Check operation type attribute and namespace imports.
- Handler not invoked? Check handler registration assembly and handler interface.
- HTTP transport fails before sending? Check `AddConquerorHttpClient()`.
- HTTP endpoint missing? Check handler registration, HTTP attribute, `AddConquerorHttpServerAspNetCore()`, endpoint mapping, and receiver disabling.
- Duplicate endpoint failure? Check generated/default HTTP path+method or signal event/tag.
- Pipeline not running? Check whether it is configured on caller side or handler side.
- Context data missing over HTTP? Check that it is transportable string data and has the right flow direction.
