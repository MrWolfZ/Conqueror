# Conqueror transports

Conqueror handlers are transport-agnostic. Transport selection happens at call sites or server mapping. Add the transport packages and services that match the capability being used.

## Package responsibilities

| Capability | Package area |
| --- | --- |
| Core in-process messages/signals/iterators | `Conqueror` |
| HTTP attributes and abstractions | `Conqueror.Transport.Http.Abstractions` |
| HTTP client sender/receiver support | `Conqueror.Transport.Http.Client` |
| ASP.NET Core HTTP server endpoints | `Conqueror.Transport.Http.Server.AspNetCore` |
| File-system transport abstractions | `Conqueror.Transport.FileSystem.Abstractions` |
| File-system transport runtime | `Conqueror.Transport.FileSystem` |

Follow the target project's version policy. Do not hard-code package versions unless the project already uses explicit versions in the file being edited.

## HTTP messages

Define HTTP-capable messages with transport attributes:

```csharp
[HttpMessage<GetTodoResponse>(HttpMethod = "GET", Version = "v1")]
public sealed partial record GetTodo(Guid TodoId);

[HttpMessage<CreateTodoResponse>(HttpMethod = "POST", Version = "v1")]
public sealed partial record CreateTodo(string Title);
```

HTTP message metadata includes method, path prefix, path, full path, version, success status code, name, and API group name. Defaults are intended to be usable, but be explicit when stable public API shape matters.

Server setup in ASP.NET Core:

```csharp
builder.Services
    .AddMessageHandlersFromAssembly(typeof(Program).Assembly)
    .AddConquerorHttpServerAspNetCore();

var app = builder.Build();

app.MapMessageEndpoints();
```

Client setup:

```csharp
services.AddConquerorHttpClient();
```

Call with an explicit HTTP transport:

```csharp
var response = await senders
    .For(GetTodo.T)
    .WithTransport(b => b.UseHttp(baseAddress))
    .Handle(new(todoId), cancellationToken);
```

If no transport is configured, Conqueror defaults to in-process.

## HTTP signals

HTTP signals are exposed via Server-Sent Events or WebSockets, depending on the attribute/package surface the app uses.

SSE endpoint shape:

```csharp
app.MapServerSentEventsSignalsEndpoint("api/signals/sse");
```

WebSocket endpoints require ASP.NET Core WebSockets middleware:

```csharp
app.UseWebSockets();
app.MapWebSocketsSignalsEndpoint("api/signals/ws");
```

Missing `UseWebSockets()` is a real runtime bug; do not skip it for WebSocket signal receivers.

Signal event type/tag defaults are generated from signal type names. Duplicate event types/tags fail at endpoint registration.

## HTTP iterators

HTTP iterator abstractions/protocol types exist, but do not present HTTP iterators as ready generic app guidance unless the target project already has a working client/server implementation. For app code, keep iterator guidance to in-process usage unless there is local evidence otherwise.

## AOT and JSON serialization

For Native AOT or trimming-sensitive apps, add source-generated serializer contexts for message/signal/iterator payloads and responses. Follow the naming convention used by Conqueror source generation:

```csharp
[JsonSerializable(typeof(GetTodo))]
[JsonSerializable(typeof(GetTodoResponse))]
internal sealed partial class GetTodoJsonSerializerContext : JsonSerializerContext;
```

If the app already has a shared serializer context pattern, follow it.

## File-system transport

File-system transport is for testing, development, or local IPC. Do not describe it as production-ready infrastructure.

It models messages/signals through files, polling, TTLs, leases, and single-instance or competing-instance receiver modes. Use it only when the user's scenario fits that model.

Typical service setup:

```csharp
services.AddConquerorFileSystemTransport();
```

Typical call-site transport selection:

```csharp
await senders
    .For(ProcessTodo.T)
    .WithTransport(b => b.UseFileSystem(directory))
    .Handle(new(todoId), cancellationToken);
```

Check the target package API for exact option names before writing nontrivial file-system transport code.

## Transport pitfalls

- Add transport services before using transport builders.
- Map HTTP endpoints explicitly on the server.
- Last `.WithTransport(...)` wins for messages/iterators.
- Duplicate HTTP path+method combinations fail.
- Duplicate signal SSE event types or WebSocket tags fail.
- Receiver disabling means endpoints are not mapped.
- Route/path configuration is usually attribute-driven, not receiver-method-driven.
- Transportable context data is string data; in-process object data does not cross process boundaries.
