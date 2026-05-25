# Conqueror middlewares

Conqueror middleware composes through explicit pipelines. Middleware receives typed context and continues by calling `ctx.Next(...)`.

## Pipeline rules

- Middleware runs in the order it is added.
- Each middleware wraps everything downstream from it.
- If middleware does not call `ctx.Next(...)`, it short-circuits the rest of the pipeline.
- Caller-side and handler-side pipelines are separate dispatches.
- Pass the current operation and cancellation token unless intentionally changing them.

Message middleware:

```csharp
pipeline.Use(ctx =>
{
    if (string.IsNullOrWhiteSpace(ctx.Message.Title))
    {
        throw new ValidationException("title is required");
    }

    return ctx.Next(ctx.Message, ctx.CancellationToken);
});
```

Signal middleware:

```csharp
pipeline.Use(ctx =>
{
    return ctx.Next(ctx.Signal, ctx.CancellationToken);
});
```

Iterator middleware returns an async stream and must enumerate the downstream stream:

```csharp
pipeline.Use(async (ctx) =>
{
    await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
    {
        yield return item;
    }
});
```

Adapt iterator syntax to the exact delegate shape in the target project.

## Extension method convention

Conqueror middlewares expose extension methods in the `Conqueror` namespace:

- `Use{Name}(...)`: add middleware
- `Configure{Name}(...)`: modify existing middleware configuration
- `Without{Name}()`: remove middleware

Use `Configure{Name}` only after the middleware has been added.

## Logging middleware

Logging supports messages and signals.

Typical use:

```csharp
public static void ConfigurePipeline(GetTodos.IPipeline pipeline) =>
    pipeline.UseLogging(c =>
    {
        c.MessagePayloadLoggingStrategy = PayloadLoggingStrategy.MinimalJson;
        c.ResponsePayloadLoggingStrategy = PayloadLoggingStrategy.Omit;
    });
```

Use payload omission for secrets, large payloads, or noisy responses. Disable expensive stack-trace capture on hot paths if the app has measured overhead.

## Authorization middleware

Authorization middleware is message-only. Do not generate `UseAuthorization` for signal or iterator pipelines.

Typical use:

```csharp
public static void ConfigurePipeline(DeleteTodo.IPipeline pipeline) =>
    pipeline.UseAuthorization(c => c.AddAuthorizationCheck(
        "authenticated",
        ctx => ctx.CurrentPrincipal?.Identity?.IsAuthenticated == true
            ? ctx.Success()
            : ctx.Unauthenticated("authentication required")));
```

Authorization reads the principal from `ConquerorContext.CurrentPrincipal`.

## Polly middleware

Polly middleware supports messages and signals.

Message pipelines use `ResiliencePipelineBuilder<TResponse>`:

```csharp
pipeline.UsePolly(b => b
    .AddRetry(new() { Delay = TimeSpan.Zero })
    .AddTimeout(TimeSpan.FromSeconds(10)));
```

Signal pipelines use non-generic `ResiliencePipelineBuilder`.

Ordering matters. If Polly is before logging, logging may run per retry. If logging is before Polly, logs may wrap only the final Polly result, depending on the exact pipeline order.

## Custom middleware

Create custom middleware when the behavior is reused. Use inline `.Use(...)` middleware for one-off logic.

A reusable message middleware generally:

1. Implements `IMessageMiddleware<TMessage, TResponse>`.
2. Accepts configuration through an options/configuration class if needed.
3. Calls `ctx.Next(...)` unless intentionally short-circuiting.
4. Exposes `UseX`, `ConfigureX`, and `WithoutX` extension methods in namespace `Conqueror`.

Keep business logic out of middleware unless it is truly cross-cutting.
