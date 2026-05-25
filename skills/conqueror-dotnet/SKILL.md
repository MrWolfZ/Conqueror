---
name: conqueror-dotnet
description: Implement, test, and debug .NET applications that use the Conqueror library for messages, signals, iterators, pipelines, context, middlewares, and transports. Use this skill whenever a user mentions Conqueror, Conqueror APIs such as [Message], [HttpMessage], [Signal], [Iterator], IMessageSenders, ISignalPublishers, IIterators, ConfigurePipeline, .T, or asks to add/use/debug Conqueror in a C#/.NET app. Do not use it for generic CQRS, message bus, event, or middleware questions unless Conqueror or its APIs are involved.
---

# Conqueror for .NET application code

Use this skill to help coding agents write correct application code with Conqueror. Optimize for small, idiomatic changes that preserve the target application's conventions.

## Core mental model

Conqueror organizes application work around source-generated operation contracts:

- **Messages** are request/response operations. Use `[Message<TResponse>]`; use `[Message]` for fire-and-forget messages with no response.
- **Signals** are publish/subscribe operations. Use `[Signal]` or a transport-specific signal attribute.
- **Iterators** are request/stream operations. Use `[Iterator<TItem>]`; treat iterators as experimental and less documented than messages/signals.
- **Pipelines** are explicit middleware chains around callers and handlers.
- **Transports** are selected at call sites or exposed by server setup. Handlers stay transport-agnostic.
- **Context** carries trace/operation IDs, principal, transportable string data, and in-process object data.

Operation types and handler types should normally be `partial`. Attributes drive source generation. The generator creates type tokens like `CreateTodo.T`, nested handler interfaces like `CreateTodo.IHandler`, and nested pipeline interfaces for messages/iterators.

## First workflow

1. Inspect the target app's existing project layout, package management, DI style, tests, and Conqueror usage if present.
2. Choose the operation model:
   - request/response: message
   - broadcast side effect: signal
   - pull-based stream: iterator, with experimental caveat
3. Add or update tests first when changing behavior. Prefer tests through Conqueror's public factories (`IMessageSenders`, `ISignalPublishers`, `IIterators`) so registration, generated APIs, pipelines, and context are exercised.
4. Define small `partial record` operation contracts with Conqueror attributes.
5. Implement `partial` handlers using generated nested interfaces such as `CreateTodo.IHandler`.
6. Register handlers and required transport/middleware services in DI.
7. Invoke operations through generated type tokens: `.For(CreateTodo.T)`, `.For(TodoCreated.T)`, `.For(StreamTodos.T)`.
8. Configure pipelines explicitly, either on handlers with `ConfigurePipeline` or at call sites with `.WithPipeline(...)`.
9. Run the narrowest relevant build/test command available in the target repo. If the repo uses Taskfiles, prefer `task build` and `task test` over direct `dotnet build/test`.

## Read references when needed

- `references/fundamentals.md`: operation contracts, handlers, DI registration, invocation, context basics.
- `references/testing.md`: test patterns for messages, signals, iterators, pipelines, context, and transports.
- `references/transports.md`: HTTP and file-system transport setup, attributes, AOT serialization, and pitfalls.
- `references/middlewares.md`: logging, authorization, Polly, middleware order, and custom middleware shape.
- `references/internals.md`: source-generation/runtime model for debugging Conqueror integration issues.

Read only the references relevant to the user task. Do not dump all reference material into the answer.

## Common implementation patterns

### Message

```csharp
[Message<CreateTodoResponse>]
public sealed partial record CreateTodo(string Title);

public sealed record CreateTodoResponse(Guid TodoId);

internal sealed partial class CreateTodoHandler(TodoStore store) : CreateTodo.IHandler
{
    public async Task<CreateTodoResponse> Handle(
        CreateTodo message,
        CancellationToken cancellationToken = default)
    {
        var todo = await store.Create(message.Title, cancellationToken);
        return new(todo.Id);
    }
}
```

Call it through the sender factory:

```csharp
var response = await senders
    .For(CreateTodo.T)
    .Handle(new("write docs"), cancellationToken);
```

### Signal

```csharp
[Signal]
public sealed partial record TodoCreated(Guid TodoId);

internal sealed partial class TodoCreatedHandler : TodoCreated.IHandler
{
    public Task Handle(TodoCreated signal, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

Publish it through the publisher factory:

```csharp
await publishers
    .For(TodoCreated.T)
    .Handle(new(todo.Id), cancellationToken);
```

### Pipeline

```csharp
public static void ConfigurePipeline(CreateTodo.IPipeline pipeline) =>
    pipeline
        .UseLogging()
        .Use(ctx => ctx.Next(ctx.Message, ctx.CancellationToken));
```

Middleware order is semantic. A middleware wraps everything downstream from it. If a middleware does not call `ctx.Next(...)`, it short-circuits the rest of the pipeline.

## Avoid these mistakes

- Do not hand-code generated `.T`, nested `IHandler`, or nested `IPipeline` members in normal app code.
- Do not call `senders.For<CreateTodo>()`; use `senders.For(CreateTodo.T)`.
- Do not register only transport services and forget handler registration.
- Do not assume one signal handler; signals can have many handlers and no handlers is a no-op for in-process publishing.
- Do not present file-system transport as production infrastructure.
- Do not present HTTP iterators as ready app guidance unless the target project already has an implementation.
- Do not apply authorization middleware to signals; Conqueror authorization middleware is message-only.
- Do not use transportable context data for arbitrary objects; transportable data is string key/value data for crossing process boundaries.
