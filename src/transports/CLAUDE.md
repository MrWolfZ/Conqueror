# Transports

Enables handlers to communicate across process boundaries while remaining transport-agnostic.

## Responsibility

Transports provide the communication layer for Conqueror handlers. Handlers are written without knowledge of how they'll be exposed, and transport configuration is applied at the call site rather than at registration time.

## Building and Testing

YOU MUST use task commands for building and testing transport projects:

```bash
# Build/test all transports
cd src/transports
task build
task test

# Build/test individual transport
cd src/transports/file-system
task build
task test

cd src/transports/http
task build
task test
```

**Forward arguments to dotnet:**

```bash
task build -- -c Release
task test -- --filter "FullyQualifiedName~FileSystem"
```

**IMPORTANT:** DO NOT use `dotnet build` or `dotnet test` directly. The task commands use the correct solution files (`Transports.sln` for aggregate, module-specific `.sln` for individual transports) with all required dependencies.

## Key Concepts

### Transport-Agnostic Design

Handlers implement core interfaces (`IMessageHandler`, `ISignalHandler`) without transport-specific code. Transport selection happens when invoking handlers:

```csharp
// In-process (default)
await senders.For(CreateOrder.T).Handle(new CreateOrder());

// HTTP
await senders.For(CreateOrder.T)
    .WithTransport(b => b.UseHttp(baseAddress))
    .Handle(new CreateOrder());
```

### Marker Interfaces

Each transport defines marker interfaces that extend core abstractions:

- `IHttpMessage<TMessage, TResponse>` extends `IMessage<TMessage, TResponse>`
- `IFileSystemMessage<TMessage, TResponse>` extends `IMessage<TMessage, TResponse>`

Source generators use these to create transport-specific handler interfaces (e.g., `CreateOrder.IHttpHandler`).

### Transport Architecture

**Client Side**: Transports implement sender/publisher factories that create transport-specific instances (`IHttpMessageSender`, `IFileSystemMessageSender`).

**Server Side**: Handlers optionally configure receivers via static methods:

```csharp
public class OrderHandler : CreateOrder.IHttpHandler
{
    static void ConfigureHttpReceiver(IHttpMessageReceiver receiver)
    {
        receiver.WithPath("api/orders");
    }
}
```

Transports provide receiver factories and runners to expose handlers over their respective protocols.

## Available Transports

### HTTP (`http/`)

REST/SSE/WebSockets transport using ASP.NET Core.

**Packages:**

- `Conqueror.Transport.Http.Abstractions` - Interfaces and attributes
- `Conqueror.Transport.Http.Client` - HTTP client
- `Conqueror.Transport.Http.Server.AspNetCore` - ASP.NET Core server

**Features:**

- Messages as REST endpoints (GET/POST)
- Signals via Server-Sent Events or WebSockets
- OpenAPI integration
- Distributed tracing
- Context data in HTTP headers

See `http/CLAUDE.md` for details.

### FileSystem (`file-system/`)

File-based transport for testing and local IPC. Not for production use.

**Packages:**

- `Conqueror.Transport.FileSystem.Abstractions` - Interfaces
- `Conqueror.Transport.FileSystem` - Implementation

**Features:**

- Messages as files on disk
- Polling-based receivers
- Single-instance or competing-instances modes
- Message TTL and leasing

See `file-system/CLAUDE.md` for details.

## Conformity Tests (`Conqueror.Transport.ConformityTests/`)

Shared test infrastructure ensuring all transports meet behavioral contracts. Transports implement test case interfaces and run shared test suites that verify:

- Message execution (messages processed correctly, responses returned)
- Context flow (ConquerorContext data flows through transport)
- Error handling (failures propagated correctly)

## Design Constraints

**Types Injector Pattern**: Transports access handler metadata without reflection using source-generated injector interfaces. This enables AOT compilation while maintaining type safety.

**Static Receiver Configuration**: Server-side configuration uses static methods on handler types to avoid reflection and support AOT scenarios.
