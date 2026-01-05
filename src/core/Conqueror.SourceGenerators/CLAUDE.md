# Conqueror.SourceGenerators

Roslyn incremental source generators that enable AOT compilation and reflection-free operation of the Conqueror library.

## Purpose

This module generates compile-time code to eliminate runtime reflection and enable Native AOT compilation. It transforms user-defined message/signal types decorated with attributes into fully-functional types with handler interfaces, pipeline support, and serialization contexts.

## What Gets Generated

### For Message/Signal Types (MessageTypeGenerator, SignalTypeGenerator)

When you write:

```csharp
[Message<OrderResponse>]
public partial record CreateOrder(string ProductId);
```

The generator produces:

- Static `IMessage<TMessage, TResponse>` implementation
- Nested `IHandler` interface for handler implementations
- Nested `IPipeline` interface for pipeline configuration
- Type metadata properties (`T`, `InvokeHandler`, `EmptyInstance`, `JsonSerializerContext`, etc.)
- Transport-specific handler interfaces if transport attributes are present (e.g., `IHttpHandler`)

### For Handler Types (MessageHandlerTypeGenerator, SignalHandlerTypeGenerator)

When you write:

```csharp
public partial class OrderHandler : CreateOrder.IHandler { ... }
```

The generator produces:

- `IMessageHandlerWithSourceGeneration` marker interface implementation
- `GetTypeInjectors()` method for reflection-free handler registration
- `[ModuleInitializer]` method for automatic handler registration at startup

## Architecture

**Generators**: Four main generators (message type, message handler type, signal type, signal handler type) following the standard Roslyn incremental generator pattern (syntax phase, semantic phase, output phase).

**Descriptors**: Immutable records capturing type metadata extracted from Roslyn symbols. Use `EquatableArray<T>` for efficient change detection in incremental compilation.

**Sources**: Static classes that generate the actual source code from descriptors using StringBuilder.

**Utilities**: Shared helpers for type introspection, attribute processing, and common generation logic.

## Key Design Decisions

**Incremental Generation**: Uses Roslyn's incremental generator API for IDE performance. Generators only re-run when relevant code changes.

**Partial Types Required**: All message/signal types and handlers must be marked `partial` to receive generated members.

**Module Initializers**: Handlers self-register at assembly load time via `[ModuleInitializer]`, eliminating manual registration.

**Transport Extensibility**: Transports define their own attributes (e.g., `[HttpMessage]`). Generators detect these and produce transport-specific handler interfaces alongside the core ones.

**AOT Serialization**: Generators detect user-provided `JsonSerializerContext` types by naming convention (`<TypeName>JsonSerializerContext`) and link them to message/signal types.

## Integration with Core

The generated code depends on abstractions in `Conqueror.Abstractions` (e.g., `IMessage<>`, `IMessageHandler<>`, `MessageTypes<>`) and integrates with the core library's registration system via `MessageHandlerTypeServiceRegistry` and `SignalHandlerTypeServiceRegistry`.

## Testing

Tests use snapshot testing: input source files in `TestCases/*/source.cs` are compiled with generators, and output is compared against expected files in `TestCases/*/expected/*.g.cs`.

## Constraints

- Targets `netstandard2.0` (required for source generators)
- Must avoid LINQ in generators for performance (see pragma warnings)
- Cannot use reflection at runtime (all type metadata must be generated)
- Special test exclusions exist for `MessageTypeGenerationTests` and `SignalTypeGenerationTests` in `Conqueror.Tests` assembly
