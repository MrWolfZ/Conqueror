# Middlewares

Cross-cutting concern implementations for Conqueror message and signal pipelines.

## Purpose

Middlewares implement reusable pipeline functionality (authorization, logging, resilience) that can be composed with any message or signal handler without modifying handler logic. Each middleware follows Conqueror's pipeline architecture, receiving execution context and calling `ctx.Next()` to pass control to the next middleware or handler.

## Building and Testing

YOU MUST use task commands for building and testing middleware projects:

```bash
# Build/test all middlewares
cd src/middlewares
task build
task test

# Build/test individual middleware
cd src/middlewares/authorization
task build
task test

cd src/middlewares/logging
task build
task test

cd src/middlewares/polly
task build
task test
```

**Forward arguments to dotnet:**

```bash
task build -- -c Release
task test -- --filter "FullyQualifiedName~Authorization"
```

**Format code:**

```bash
task fmt              # Format all files in current scope
task fmt:check        # Check formatting (for CI)
```

**IMPORTANT:** DO NOT use `dotnet build` or `dotnet test` directly. The task commands use the correct solution files (`Middlewares.sln` for aggregate, module-specific `.sln` for individual middlewares) with all required dependencies.

## Architecture

Middlewares use the Chain of Responsibility pattern:

- Implement `IMessageMiddleware<TMessage, TResponse>` or `ISignalMiddleware<TSignal>`
- Receive immutable context struct with message, service provider, ConquerorContext, and transport metadata
- Call `ctx.Next()` to continue execution
- Can execute logic before and/or after the handler

All middlewares expose three extension methods in the `Conqueror` namespace:

- `Use{Name}(config)` - Add middleware with configuration
- `Configure{Name}(config)` - Modify existing middleware configuration
- `Without{Name}()` - Remove middleware from pipeline

## Available Middlewares

### Authorization

**Package**: `Conqueror.Middleware.Authorization`

Claim-based authorization checks before handler execution. Supports multiple named checks (sync/async), accesses `ClaimsPrincipal` from `ConquerorContext.CurrentPrincipal`, and throws `MessageAuthorizationFailedException` on failure.

**Note**: Only supports messages (not signals). Signal authorization is planned.

See `authorization/CLAUDE.md` for details.

### Logging

**Package**: `Conqueror.Middleware.Logging`

Structured logging of message/signal lifecycle (pre-execution, post-execution, exceptions). Configurable log levels, payload serialization strategies (Omit, Raw, MinimalJson, IndentedJson), and custom hooks. Performance-optimized with log level checks and AOT-compatible JSON serialization.

See `logging/CLAUDE.md` for details.

### Polly

**Package**: `Conqueror.Middleware.Polly`

Wraps handler execution with Polly resilience patterns (retry, timeout, circuit-breaker, hedging, rate limiting). Uses Polly's `ResiliencePipelineBuilder<TResponse>` for configuration. Middleware placement in pipeline determines which operations are wrapped (place before logging to log each retry attempt, after to log only final result).

See `polly/CLAUDE.md` for details.

## Design Decisions

**Configuration via required properties**: Each middleware uses a configuration class with `required` init properties set by pipeline extension methods. This ensures compile-time safety while allowing runtime customization.

**Transport-agnostic**: Middlewares work with both in-process and remote transports. Context includes `TransportType` for transport-specific behavior (e.g., logging distinguishes sender vs. receiver).

**Signal support**: Logging and Polly support both messages and signals. Authorization currently supports only messages.

**Performance**: Middlewares check preconditions (e.g., log levels) before expensive operations (e.g., JSON serialization) to minimize overhead when disabled.

## Creating Custom Middleware

1. Implement `IMessageMiddleware<TMessage, TResponse>` and/or `ISignalMiddleware<TSignal>`
2. Create configuration class with `required` properties
3. Add extension methods in `Conqueror` namespace following naming conventions
4. Keep middleware class `internal sealed`
5. Call `ctx.Next()` to continue execution

See existing middlewares for reference implementations.
