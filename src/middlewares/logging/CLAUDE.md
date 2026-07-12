# Conqueror.Middleware.Logging

Structured logging middleware for message and signal pipelines.

## Responsibility

Logs execution lifecycle of messages and signals at three phases:

1. **Pre-execution**: Before handler execution (payload, message/signal ID, trace ID)
2. **Post-execution**: After successful execution (response payload, execution time)
3. **Exception**: When handler throws (exception details, full stack trace)

Supports both message and signal pipelines with identical configuration patterns.

## Building and Testing

YOU MUST use task commands for building and testing:

```bash
cd src/middlewares/logging
task build
task test
```

Forward arguments: `task test -- --filter "FullyQualifiedName~Logging"`

Format code: `task fmt`

**IMPORTANT:** DO NOT use `dotnet build` or `dotnet test` directly. Use the task commands.

## Key Features

**Configurable Log Levels**: Set different log levels for each execution phase (defaults: Information for pre/post, Error for exceptions).

**Payload Serialization**: Four strategies via `PayloadLoggingStrategy` enum:

- `Omit`: Skip payload logging
- `Raw`: Log object reference
- `MinimalJson`: Compact JSON (default)
- `IndentedJson`: Pretty-printed JSON

Can be configured statically or dynamically via factory functions.

**Custom Hooks**: Pre/post/exception hooks that receive context and can:

- Perform custom logging using the provided `ILogger`
- Return `false` to suppress default log message
- Access message/signal, transport type, timing, and other execution metadata

**Custom Logger Category**: Configure logger category per message/signal via `LoggerCategoryFactory` (defaults to message/signal type name).

**Full Stack Trace Capture**: Captures stack trace at middleware entry to preserve full call stack in exception logs (exceptions during stack unwinding only contain frames from throw point to middleware). Can be disabled via `StackTraceCaptureIsDisabled` for performance-critical paths.

## Architecture Integration

**Log Level Optimization**: Checks `logger.IsEnabled(level)` before serializing payloads to avoid unnecessary work when log level is disabled.

**AOT Compatibility**: Uses `IMessage.JsonSerializerContext` / `ISignal.JsonSerializerContext` for payload serialization. Falls back to reflection-based serialization when context unavailable (non-AOT scenarios).

**Transport Awareness**: Logs include transport type (in-process vs named transport) and role (sender/receiver for messages, publisher/receiver for signals).

**Robust Error Handling**: Hook exceptions never fail execution; they're logged and swallowed to ensure logging doesn't break business logic.

## Design Decisions

**Stack Trace Trade-off**: By default, captures stack trace on every execution for better debuggability at the cost of performance. This is an intentional trade-off favoring developer experience, but can be disabled for hot paths.

**WrappingException Pattern**: Wraps exceptions with captured stack trace to show full call stack (from middleware invocation point) rather than just partial trace (from throw point to middleware).

**Logger Caching**: Uses `ConcurrentDictionary` to cache logger type lookups, avoiding repeated reflection costs.

**Hook Resilience**: All hook exceptions are caught and logged separately to prevent user code from breaking the logging flow.

## Usage Pattern

```csharp
// Basic usage
pipeline.UseLogging();

// Custom configuration
pipeline.UseLogging(c => c
    .WithPreExecutionLogLevel(LogLevel.Debug)
    .WithMessagePayloadLoggingStrategy(PayloadLoggingStrategy.IndentedJson)
    .WithResponsePayloadLoggingStrategy(PayloadLoggingStrategy.Omit)
    .WithLoggerCategoryFactory(msg => $"Orders.{msg.OrderType}")
    .WithPreExecutionHook(ctx => {
        // Custom logging, return false to skip default log
        return true;
    }));

// Disable stack trace capture for performance
pipeline.UseLogging(c => c.StackTraceCaptureIsDisabled = true);
```

See parent `middlewares/CLAUDE.md` for general middleware architecture patterns.
