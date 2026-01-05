# Conqueror.Abstractions

Core abstractions defining the public API surface for the Conqueror messaging library. All other packages depend on this.

## Responsibility

Provides transport-agnostic interfaces for:

- Messages (request/response pattern)
- Signals (publish/subscribe pattern)
- Iterators (request/stream pattern)
- Middleware pipelines
- Execution context management

All types use the root `Conqueror` namespace for a unified developer experience.

## Module Structure

```txt
Messaging/    - Message handling (request/response)
Signalling/   - Signal handling (publish/subscribe)
Iterating/    - Iterator handling (request/stream)
Context/      - Ambient execution context
```

## Key Concepts

### Messages (`Messaging/`)

**Core interfaces:**

- `IMessage<TMessage, TResponse>` - Base interface for message types, source-generated with static abstract members for reflection-free operation
- `IMessageHandler<TMessage, TResponse, TIHandler>` - Handler interface using self-referential generics (CRTP pattern)
- `IMessageSenders` - Entry point for sending messages with configurable transport and pipeline
- `IMessagePipeline<TMessage, TResponse>` - Fluent middleware chain configuration
- `IMessageMiddleware<TMessage, TResponse>` - Middleware interface
- `MessageMiddlewareContext<TMessage, TResponse>` - Value type passed through middleware chain

**Attributes:**

- `[Message]` / `[Message<TResponse>]` - Marks types for source generation

**Note:** Handler interfaces (`TMessage.IHandler`) are source-generated and use CRTP to maintain type identity through pipelines.

### Signals (`Signalling/`)

Parallel structure to messaging but for fire-and-forget operations:

- `ISignal<TSignal>` - Base interface for signal types
- `ISignalHandler<TSignal, TIHandler>` - Handler interface
- `ISignalPublishers` - Entry point for publishing signals
- `IAggregateSignalPublisher<TSignal>` - Publisher with broadcasting strategy support (sequential/parallel)
- `ISignalBroadcastingStrategy` - Strategy interface for invoking multiple handlers

**Attributes:**

- `[Signal]` - Marks types for source generation

### Iterators (`Iterating/`)

Parallel structure to messaging but for streaming operations:

- `IIterator<TIterator, TItem>` - Base interface for iterator types
- `IIteratorHandler<TIterator, TItem, TIHandler>` - Handler interface
- `IIteratorClients` - Entry point for iterating with configurable transport and pipeline
- `IIteratorPipeline<TIterator, TItem>` - Fluent middleware chain configuration
- `IIteratorMiddleware<TIterator, TItem>` - Middleware interface
- `IteratorMiddlewareContext<TIterator, TItem>` - Value type passed through middleware chain

**Attributes:**

- `[Iterator<TItem>]` - Marks types for source generation

**Note:** Handler interfaces (`TIterator.IHandler`) are source-generated and return `IAsyncEnumerable<TItem>`.

### Context (`Context/`)

Ambient execution context flowing through operations:

**ConquerorContext** encapsulates:

- `TraceId` - Distributed tracing ID (from Activity or generated)
- `MessageId` / `SignalId` / `IteratorId` - Current operation ID
- `CurrentPrincipal` - Security principal (ClaimsPrincipal)
- `TransportableData` - Cross-process data (string key/value) with flow direction control
- `InProcessData` - In-process data (object key/value)

**ConquerorContextDataFlowDirection** controls data propagation:

- Downstream - Parent to child (default)
- Upstream - Child to parent
- Bidirectional - Both directions

**IConquerorContextAccessor** provides access to ambient context (AsyncLocal-based).

## Design Patterns

### Self-Referential Generics (CRTP)

Handler interfaces maintain type identity through the pipeline:

```csharp
IMessageHandler<TMessage, TResponse, TIHandler>
    where TIHandler : IMessageHandler<TMessage, TResponse, TIHandler>
```

This ensures the fluent API returns the correct handler type at each step.

### Types Injector Pattern

Enables reflection-free handler access for transports and framework internals:

- `IMessageHandlerTypesInjector` - Public interface exposing `MessageType`
- `ICoreMessageHandlerTypesInjector` - Internal extension with generic injection capability
- `ICoreMessageHandlerTypesInjectable<TArg, TResult>` - Injectable interface for type-safe callbacks

Transports use injectors to access handler type information without runtime reflection, critical for AOT scenarios.

### Middleware Context as Value Type

`MessageMiddlewareContext<TMessage, TResponse>` is a `readonly record struct` to minimize allocations. Internal state is captured in a nested class to avoid excessive copying when passed by value.

## Important Constraints

- All messages/signals must be `partial` classes/records for source generation
- Messages/signals must be reference types (`class`)
- Response types should be immutable (records preferred)
- Middleware execution appears synchronous but is async via `ctx.Next()`
- Context data flow direction matters in distributed scenarios (affects serialization and propagation)
- Handler interfaces are always named `IHandler` (validated at runtime via `MessageTypes<>` / `SignalTypes<>`)

## Dependencies

- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI integration
- `System.Text.Json` - JSON serialization (AOT-compatible)

No other runtime dependencies to support AOT and minimize coupling.
