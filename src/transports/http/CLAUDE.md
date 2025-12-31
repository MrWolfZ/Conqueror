# HTTP Transport

Provides REST, Server-Sent Events (SSE), and WebSockets transport for Conqueror handlers.

## Packages

- **Conqueror.Transport.Http.Abstractions** - Marker interfaces, attributes, and configuration types
- **Conqueror.Transport.Http.Client** - Client-side sender/publisher implementations using HttpClient
- **Conqueror.Transport.Http.Server.AspNetCore** - Server-side receiver implementations using ASP.NET Core endpoints

## Key Concepts

### Messages as REST Endpoints

Messages are exposed as HTTP endpoints with configurable methods, paths, and versioning.

**Default behavior**:

- POST requests with JSON body for messages
- GET requests with query string parameters for messages marked with `HttpMethod = "GET"`
- Path defaults to camelCase message type name (e.g., `CreateOrder` → `createOrder`)
- Path prefix defaults to `api`
- Success status codes: 200 for responses with data, 204 for `UnitMessageResponse`

**Key types**:

- `IHttpMessage<TMessage, TResponse>` - Marker interface for HTTP-transportable messages
- `HttpMessageAttribute` - Configures HTTP method, path, version, status codes, OpenAPI metadata
- `IHttpMessageSender<TMessage, TResponse>` - Client-side message sender
- `IHttpMessageReceiver` - Server-side receiver configuration (path overrides, disabling, API description)

### Signals via SSE and WebSockets

Signals can be published/received over persistent connections.

**Server-Sent Events (SSE)**:

- Unidirectional server-to-client streaming
- Event types default to camelCase signal type name
- `HttpSseSignalAttribute` configures event type

**WebSockets**:

- Bidirectional communication
- Signal tags default to camelCase signal type name
- `HttpWebSocketsSignalAttribute` configures tag
- Protocol v1 supports multiplexing multiple signal types over a single connection

**Key types**:

- `IHttpSseSignal<TSignal>` / `IHttpWebSocketsSignal<TSignal>` - Marker interfaces
- `IHttpSseSignalPublisher<TSignal>` / `IHttpWebSocketsSignalPublisher<TSignal>` - Client-side publishers
- `IHttpSseSignalReceiver` / `IHttpWebSocketsSignalReceiver` - Server-side receiver configuration

### Context Propagation

ConquerorContext data flows through HTTP headers:

- Trace IDs in W3C traceparent headers
- Custom context data in `Conqueror-Context-*` headers
- Supports downstream, upstream, and bidirectional data flow

### Serialization

Uses System.Text.Json with AOT-compatible source generation:

- Messages use `IMessage.JsonSerializerContext` for request/response serialization
- GET requests serialize to query strings instead of JSON bodies
- Signals use `ISignal.JsonSerializerContext`

## Design Constraints

**Path routing**: Messages define their own routes via attributes or `IHttpMessageReceiver` configuration. The server maps all configured handlers at startup.

**OpenAPI integration**: Server exposes handlers in OpenAPI specs by default. Handlers can opt out via `receiver.OmitFromApiDescription()`.

**Handler configuration**: Handlers optionally implement static `ConfigureHttpReceiver` / `ConfigureHttpSseReceiver` / `ConfigureHttpWebSocketsReceiver` methods to customize receiver behavior.

**AOT compatibility**: No runtime reflection. All type metadata comes from source-generated injectors and marker interfaces.

## Fitting into Architecture

The HTTP transport sits at the boundary between Conqueror's core abstractions and ASP.NET Core / HttpClient:

- **Abstractions** define marker interfaces (`IHttpMessage`, etc.) that extend core message/signal interfaces
- **Client** wraps HttpClient to send HTTP requests and handle responses
- **Server** registers ASP.NET Core endpoints that invoke Conqueror handlers

Handlers remain transport-agnostic. The HTTP transport is selected at the call site:

```csharp
// Client-side
await senders.For(CreateOrder.T)
    .WithTransport(b => b.UseHttp(baseAddress))
    .Handle(new CreateOrder());

// Server-side (automatic)
app.MapConquerorHttpEndpoints(); // Discovers all IHttpMessage handlers
```
