# Core

Core abstractions, implementations, source generators, and tests for the Conqueror library.

## Purpose

Contains the foundational types and runtime for Conqueror's messaging, signalling, and iterating patterns. Includes source generators that provide AOT-compatible code generation for handler interfaces and type metadata.

## Building and Testing

YOU MUST use task commands for building and testing core projects:

```bash
# Build/test all core projects
cd src/core
task build
task test

# Test only source generators
task test:generators

# Run benchmarks
task benchmarks:run

# Run AOT tests
task test:aot
```

**Forward arguments to dotnet:**

```bash
task build -- -c Release
task test -- --filter "FullyQualifiedName~Messaging"
task benchmarks:run -- "message-bench"
```

**Format code:**

```bash
task fmt              # Format all files in current scope
task fmt:check        # Check formatting (for CI)
```

**IMPORTANT:** DO NOT use `dotnet build` or `dotnet test` directly. The task commands use the correct solution file (`core.sln`) with all required dependencies.

## Projects

### Conqueror.Abstractions

Core interfaces and types for all Conqueror patterns:

- **Messaging**: `IMessage<TResponse>`, `IMessageHandler<,>`, `IMessagePipeline<,>`
- **Signalling**: `ISignal`, `ISignalHandler<>`, `ISignalPipeline<>`
- **Iterating**: `IIterator<TItem>`, `IIteratorHandler<,>`, `IIteratorPipeline<,>`
- **Context**: `ConquerorContext`, context data flow abstractions

All interfaces are transport-agnostic and designed for AOT compatibility.

### Conqueror

Core runtime implementation:

- Dispatchers (message, signal, iterator)
- Pipeline execution
- Context management
- Handler registration and DI integration
- Sender/publisher/client factories

### Conqueror.SourceGenerators

Roslyn incremental source generators that create:

- Handler interfaces for each message/signal/iterator type (e.g., `CreateOrder.IHandler`)
- Type metadata for AOT scenarios
- Pipeline configuration interfaces
- Transport-specific handler interfaces

Targets `netstandard2.0` for Roslyn compatibility.

### Conqueror.Tests

Comprehensive test suite for core functionality covering all messaging, signalling, and iterating scenarios.

### Conqueror.Tests.AOT

AOT (Ahead-of-Time) compilation tests ensuring the core library works correctly with Native AOT scenarios. Only tests core functionality; middleware and transport AOT compatibility is tested in their respective projects.

### Conqueror.Benchmarks

Performance benchmarks for core operations using BenchmarkDotNet. Only benchmarks core functionality; middleware and transport performance is benchmarked in their respective projects.

### Conqueror.SourceGenerators.Tests

Tests for source generator behavior using Roslyn's testing infrastructure.

## Architecture Notes

**Self-Referential Generics**: Handler interfaces use CRTP pattern to maintain type identity through pipeline chains.

**Source Generation Strategy**: Heavy reliance on generated code enables AOT compilation while providing type-safe, ergonomic APIs.

**Transport Independence**: All core types are agnostic to transport implementation; transport configuration happens at call sites.
