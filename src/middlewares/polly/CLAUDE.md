# Conqueror.Middleware.Polly

Resilience middleware integrating Polly v8+ with Conqueror pipelines.

## Purpose

Wraps message and signal execution in a Polly `ResiliencePipeline`, enabling standard resilience patterns:

- Retry with exponential backoff
- Timeout
- Circuit breaker
- Hedging
- Rate limiting
- Fallback

## Building and Testing

YOU MUST use task commands for building and testing:

```bash
cd src/middlewares/polly
task build
task test
```

Forward arguments: `task test -- --filter "FullyQualifiedName~Polly"`

**IMPORTANT:** DO NOT use `dotnet build` or `dotnet test` directly. Use the task commands.

## Key Concepts

**ResiliencePipelineBuilder**: The middleware exposes Polly's fluent builder directly via configuration. Users compose resilience strategies using Polly's native API.

**Pass-through behavior**: If no resilience pipeline is configured (`UsePolly()` with no arguments), the middleware passes through directly without overhead.

**Pipeline wrapping**: The resilience pipeline wraps `ctx.Next()` execution, meaning it applies to all downstream middlewares and the final handler.

## Configuration

```csharp
public sealed class PollyMessageMiddlewareConfiguration<TMessage, TResponse>
{
    public ResiliencePipelineBuilder<TResponse>? ResiliencePipelineBuilder { get; set; }
}

public sealed class PollySignalMiddlewareConfiguration<TSignal>
{
    public ResiliencePipelineBuilder? ResiliencePipelineBuilder { get; set; }
}
```

Message middleware uses generic `ResiliencePipelineBuilder<TResponse>` for typed response handling. Signal middleware uses non-generic `ResiliencePipelineBuilder` since signals have no response.

## Usage

```csharp
// Single strategy
pipeline.UsePolly(builder => builder
    .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 }));

// Composed strategies (executed in order)
pipeline.UsePolly(builder => builder
    .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 })
    .AddTimeout(TimeSpan.FromSeconds(10)));

// Configure after initial setup
pipeline.ConfigurePolly(builder => builder
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions { ... }));

// Pass-through (no resilience)
pipeline.UsePolly();
```

## Middleware Order Considerations

Pipeline order affects what gets wrapped by resilience:

```csharp
// Logging OUTSIDE retry - logs each retry attempt
pipeline
    .UseLogging()
    .UsePolly(b => b.AddRetry(...));

// Logging INSIDE retry - logs only final result
pipeline
    .UsePolly(b => b.AddRetry(...))
    .UseLogging();
```

## Architecture Integration

This middleware is a thin wrapper around Polly's `ResiliencePipeline`. It builds the pipeline from the configured builder and executes `ctx.Next()` within `ResiliencePipeline.ExecuteAsync()`.

The middleware delegates all resilience strategy configuration to Polly's native API, maintaining a clean separation of concerns and avoiding duplication of Polly's comprehensive documentation.

## Dependencies

- `Polly.Core` - Polly v8+ resilience library
