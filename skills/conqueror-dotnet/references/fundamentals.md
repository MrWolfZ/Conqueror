# Conqueror fundamentals

## Operation models

Use Conqueror when application behavior should be expressed as typed operations:

| Model | Use for | Contract attribute | Handler return |
| --- | --- | --- | --- |
| Message | request/response | `[Message<TResponse>]` | `Task<TResponse>` |
| Message without response | command-like side effect | `[Message]` | `Task` |
| Signal | publish/subscribe | `[Signal]` | `Task` |
| Iterator | request/stream | `[Iterator<TItem>]` | `IAsyncEnumerable<TItem>` |

Messages and signals are the primary stable app-facing models. Iterators exist in core and tests, but treat them as experimental unless the target project already uses them.

## Source-generated API

The user writes:

```csharp
[Message<GetTodoResponse>]
public sealed partial record GetTodo(Guid TodoId);

public sealed record GetTodoResponse(Guid TodoId, string Title);

internal sealed partial class GetTodoHandler(TodoStore store) : GetTodo.IHandler
{
    public async Task<GetTodoResponse> Handle(
        GetTodo message,
        CancellationToken cancellationToken = default)
    {
        var todo = await store.Get(message.TodoId, cancellationToken);
        return new(todo.Id, todo.Title);
    }
}
```

The generator provides:

- `GetTodo.T` type token for factory APIs.
- `GetTodo.IHandler` nested handler interface.
- `GetTodo.IPipeline` nested pipeline interface for messages.
- Handler metadata used for registration and transports.
- Optional transport-specific interfaces when transport attributes are used.

Use generated interfaces instead of generic core interfaces in app handlers. This keeps IDE navigation and type inference useful.

## Dependency injection

Typical registration:

```csharp
services
    .AddMessageHandlersFromAssembly(typeof(Program).Assembly)
    .AddSignalHandlersFromAssembly(typeof(Program).Assembly);
```

Register the app services handlers depend on as usual:

```csharp
services.AddSingleton<TodoStore>();
```

Use narrower individual registration only when the target app already does so:

```csharp
services.AddMessageHandler<GetTodoHandler>();
```

Messages and iterators are effectively one handler per operation type. Signals can have multiple handlers for the same signal type.

## Invocation

Inject factory entry points into app services, handlers, endpoints, or tests:

```csharp
public sealed class TodoService(IMessageSenders senders)
{
    public Task<GetTodoResponse> Get(Guid todoId, CancellationToken cancellationToken) =>
        senders.For(GetTodo.T).Handle(new(todoId), cancellationToken);
}
```

Signals use the same generated handler-shaped proxy:

```csharp
await publishers.For(TodoCreated.T).Handle(new(todo.Id), cancellationToken);
```

Iterators return streams:

```csharp
await foreach (var todo in iterators.For(StreamTodos.T).Handle(new(), cancellationToken))
{
    // consume item
}
```

## Handler pipelines

Messages and iterators can configure type-specific handler pipelines:

```csharp
public static void ConfigurePipeline(GetTodo.IPipeline pipeline) =>
    pipeline.UseLogging();
```

Signals use a generic static pipeline hook because one signal handler type can handle multiple signal types:

```csharp
static void ISignalHandler.ConfigurePipeline<TSignal>(ISignalPipeline<TSignal> pipeline) =>
    pipeline.UseLogging();
```

Call-site pipelines run around the caller-side sender/publisher/client:

```csharp
await senders
    .For(GetTodo.T)
    .WithPipeline(p => p.UseLogging())
    .Handle(new(todoId), cancellationToken);
```

The caller-side and handler-side pipelines are separate. This is intentional: one wraps sending/publishing/client work, the other wraps receiver/handler work.

## Context data

`ConquerorContext` carries:

- `TraceId`
- current `MessageId`, `SignalId`, or `IteratorId`
- `CurrentPrincipal`
- `TransportableData`: string key/value data that can cross transports
- `InProcessData`: object data that stays in-process

Use transportable data for correlation, tenant IDs, or auth-related values that must cross process boundaries. Use in-process data for scoped object values and local flags.

Data flow direction matters:

- downstream: parent to child
- upstream: child to parent
- bidirectional: both

For loop-prevention flags and other local guards, prefer `InProcessData`.
