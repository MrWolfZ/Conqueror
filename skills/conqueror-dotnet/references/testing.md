# Testing Conqueror application code

Conqueror is designed to be tested through app-level dependency injection without hiding control flow. Prefer tests that exercise the same public factories app code uses.

## Default test shape

Use a small service provider:

```csharp
var services = new ServiceCollection()
    .AddSingleton<TodoStore>()
    .AddMessageHandlersFromAssembly(typeof(CreateTodoHandler).Assembly)
    .AddSignalHandlersFromAssembly(typeof(TodoCreatedHandler).Assembly);

await using var provider = services.BuildServiceProvider();
```

Then call through Conqueror:

```csharp
var senders = provider.GetRequiredService<IMessageSenders>();

var response = await senders
    .For(CreateTodo.T)
    .Handle(new("write tests"));

response.TodoId.Should().NotBeEmpty();
```

This validates:

- source-generated `.T` and nested handler interfaces
- handler registration
- DI resolution
- pipelines
- context propagation
- configured transports when used

## Unit tests vs integration tests

Prefer fast tests for business logic. If a handler has substantial logic, extract that logic into a service and unit test the service directly.

Still add at least one Conqueror-facing test for each important operation shape. Directly constructing a handler can be useful, but it does not validate Conqueror registration, generated APIs, or middleware.

## Testing messages

Test request/response behavior through `IMessageSenders`:

```csharp
var result = await provider
    .GetRequiredService<IMessageSenders>()
    .For(GetTodo.T)
    .Handle(new(todoId));
```

For `[Message]` without response, assert the side effect:

```csharp
await senders.For(RebuildIndex.T).Handle(new());

store.RebuildCount.Should().Be(1);
```

## Testing signals

Signals can have zero, one, or many handlers. In-process publishing with no handlers is a no-op.

Test signal side effects through `ISignalPublishers`:

```csharp
await provider
    .GetRequiredService<ISignalPublishers>()
    .For(TodoCreated.T)
    .Handle(new(todoId));

metrics.CreatedTodoCount.Should().Be(1);
```

When a signal handler sends messages or publishes other signals, test the resulting behavior, not implementation details.

## Testing iterators

Consume the async stream:

```csharp
var items = new List<Todo>();

await foreach (var item in provider
    .GetRequiredService<IIterators>()
    .For(StreamTodos.T)
    .Handle(new()))
{
    items.Add(item);
}

items.Should().HaveCount(2);
```

Iterator middleware should preserve cancellation and enumerate `ctx.Next(...)` correctly.

## Testing pipelines and context

Use pipeline tests when behavior depends on middleware order, short-circuiting, or context:

```csharp
public static void ConfigurePipeline(CreateTodo.IPipeline pipeline) =>
    pipeline.Use(ctx =>
    {
        ctx.ConquerorContext.InProcessData.Set("seen-create-todo", true);
        return ctx.Next(ctx.Message, ctx.CancellationToken);
    });
```

Assert observable effects. Avoid tests that merely verify a mocked middleware was called; they are brittle and do not prove Conqueror behavior.

## Testing HTTP transport

For HTTP message endpoints, use the target app's ASP.NET Core test-host pattern when available. Validate:

- services are registered with `AddConquerorHttpServerAspNetCore()`
- message endpoints are mapped with `MapMessageEndpoints()`
- HTTP client usage has `AddConquerorHttpClient()`
- expected status codes and JSON payloads
- well-known error handling if used

For Native AOT or trimming-sensitive code, include serializer-context coverage for request/response types.

## Validation checklist

- Run the narrowest relevant tests.
- Run formatting if the repo expects it.
- If the target repo has a Taskfile, prefer `task test` / `task build` in the narrowest relevant directory.
- Do not claim Conqueror behavior works if only direct handler tests were run.
