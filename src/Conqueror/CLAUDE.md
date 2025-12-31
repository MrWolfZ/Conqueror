# Conqueror

Core runtime implementation of the Conqueror messaging library.

## Purpose

This module implements the abstractions defined in `Conqueror.Abstractions`, providing the runtime infrastructure for dispatching messages and signals through middleware pipelines and transports.

Key responsibilities:

- Dispatch messages and signals through pipelines
- Execute middleware chains
- Manage execution context (trace IDs, message/signal IDs, security principal)
- Provide in-process transport implementation
- Register and resolve handlers from DI

## Module Structure

```txt
Conqueror/
├── Messaging/          # Message (request/response) infrastructure
├── Signalling/         # Signal (pub/sub) infrastructure
└── Context/            # Execution context management
```

## Key Components

### Messaging

- **MessageDispatcher** - Orchestrates message execution: clones context, generates message ID, builds pipeline, resolves transport
- **MessagePipeline** - Middleware chain with fluent API for adding/removing/configuring middleware
- **InProcessMessageSender** - Default transport that resolves and invokes handlers from DI
- **MessageHandlerRegistry** - Stores handler metadata and creates invokers for transports

### Signalling

Parallel structure to messaging with signal-specific features:

- **SignalDispatcher** - Orchestrates signal execution
- **SignalPipeline** - Signal middleware chain
- **InProcessSignalPublisher** - Default transport for in-process signal handling
- **AggregateSignalPublisher** - Publishes to multiple handlers with configurable strategy (sequential/parallel)

### Context

- **DefaultConquerorContext** - Manages trace ID, message/signal ID, principal, and context data
- **DefaultConquerorContextAccessor** - Provides ambient context via `AsyncLocal`
- Context data has two variants: in-process (any object) and transportable (string key/value pairs for cross-process)
- Supports configurable flow direction (downstream, upstream, bidirectional)

## Integration

The module integrates with `Microsoft.Extensions.DependencyInjection`:

```csharp
services.AddConqueror();  // Registers all core services

// Handler registration (via source-generated module initializers)
services.AddMessageHandlersFromAssembly(assembly);
services.AddSignalHandlersFromAssembly(assembly);
```

## Design Notes

### Context Cloning

When dispatching, context is cloned to create an isolated execution scope. This preserves upstream data while allowing downstream modifications. Uses copy-on-write for internal data structures for performance.

### Transport Resolution

Transport is resolved at call site (not registration). If no transport is configured, defaults to in-process:

```csharp
// Explicit transport
await senders.For(MyMessage.T)
    .WithTransport(b => b.UseHttp())
    .Handle(message);

// Defaults to in-process
await senders.For(MyMessage.T).Handle(message);
```

### Pipeline Execution

Middleware chains use chain-of-responsibility pattern via context structs. Each middleware calls `ctx.Next()` to continue the chain. Final link invokes the transport sender/publisher.

### Broadcasting Strategies

Signals support multiple handlers with configurable execution:

- **Sequential** - Handlers execute one by one (default)
- **Parallel** - Handlers execute concurrently

### AOT Compatibility

No runtime reflection for handler discovery. All metadata is provided via source-generated type injectors and module initializers.
