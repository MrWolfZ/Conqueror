# Conqueror.Middleware.Authorization

Claim-based authorization middleware for Conqueror message pipelines.

## Purpose

Executes authorization checks before message handler execution. Reads `ClaimsPrincipal` from
`ConquerorContext.CurrentPrincipal` and runs user-defined authorization logic. Throws
`MessageAuthorizationFailedException` if any check fails.

## Building and Testing

YOU MUST use task commands for building and testing:

```bash
cd src/middlewares/authorization
task build
task test
```

Forward arguments: `task test -- --filter "FullyQualifiedName~Authorization"`

Format code: `task fmt`

**IMPORTANT:** DO NOT use `dotnet build` or `dotnet test` directly. Use the task commands.

## Key Types

**`MessageAuthorizationContext<TMessage, TResponse>`** - Context passed to authorization check
delegates containing the message, principal, service provider, and cancellation token. Provides
helper methods `Success()`, `Unauthenticated(details)`, and `Unauthorized(details)` for returning
results.

**`AuthorizationResult`** - Abstract result type with two implementations:
`AuthorizationSuccessResult` and `AuthorizationFailureResult` (contains details and reason).

**`MessageAuthorizationFailedException`** - Exception thrown when authorization fails. Contains the
failure result, message payload, and transport type. Uses well-known reasons "Unauthenticated" or
"Unauthorized" for error classification.

**`AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse>`** - Configuration allowing
multiple named authorization checks (sync or async). Checks can be added or removed by ID.

## Usage

```csharp
// Add authorization with multiple checks
pipeline.UseAuthorization(c => c
    .AddAuthorizationCheck("authenticated", ctx =>
        ctx.Principal.Identity?.IsAuthenticated == true
            ? ctx.Success()
            : ctx.Unauthenticated("Not authenticated"))
    .AddAuthorizationCheck("admin-role", ctx =>
        ctx.Principal.IsInRole("Admin")
            ? ctx.Success()
            : ctx.Unauthorized("Admin role required"))
    .AddAuthorizationCheck("async-check", async ctx =>
    {
        var service = ctx.ServiceProvider.GetRequiredService<IPermissionService>();
        return await service.HasPermission(ctx.Principal, ctx.Message)
            ? ctx.Success()
            : ctx.Unauthorized("Insufficient permissions");
    }));

// Configure existing middleware
pipeline.ConfigureAuthorization(c => c.AddAuthorizationCheck(...));

// Remove authorization
pipeline.WithoutAuthorization();
```

## Behavior

- Executes all authorization checks sequentially in the order they were added
- Throws `MessageAuthorizationFailedException` immediately if any check fails (short-circuits)
- Calls `ctx.Next()` to continue pipeline if all checks pass

## Integration

The middleware reads from `ConquerorContext.CurrentPrincipal`. Set it upstream using:

```csharp
conquerorContext.SetCurrentPrincipal(claimsPrincipal);
```

## Architecture Notes

- **Messages only**: No signal support (signals are fire-and-forget, authorization typically applies
  to request/response)
- **Named checks**: IDs allow pipeline configuration to remove or replace specific checks
- **No caching**: Checks execute on every invocation (design authorization logic accordingly)

## Status

**Experimental** - API may change.
