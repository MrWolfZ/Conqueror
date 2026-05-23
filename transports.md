# Transport architecture findings

## Scope and files read

- `src/transports/CLAUDE.md`
- `src/transports/http/CLAUDE.md`
- `src/transports/file-system/CLAUDE.md`
- `src/transports/http/Conqueror.Transport.Http.Abstractions/Messaging/IHttpMessage.cs`
- `src/transports/http/Conqueror.Transport.Http.Abstractions/Messaging/HttpMessageQueryStringSerializer.cs`
- `src/transports/http/Conqueror.Transport.Http.Server.AspNetCore/Messaging/ConquerorHttpServerMessagingEndpointRouteBuilderExtensions.cs`
- `src/transports/http/Conqueror.Transport.Http.Server.AspNetCore/WebSockets/WebSocketEndpoint.cs`
- `src/transports/http/Conqueror.Transport.Http.Abstractions/Signalling/WebSockets/HttpWebSocketsSignalProtocolV1.cs`
- `src/transports/http/Conqueror.Transport.Http.Abstractions/Iterating/WebSockets/HttpWebSocketsIteratorProtocolV1.cs`
- `src/transports/file-system/Conqueror.Transport.FileSystem.Abstractions/Messaging/IFileSystemMessage.cs`
- `src/transports/file-system/Conqueror.Transport.FileSystem.Abstractions/Signalling/IFileSystemSignal.cs`
- `src/transports/file-system/Conqueror.Transport.FileSystem/Messaging/FileSystemMessageReceiverRunner.cs`
- `src/transports/file-system/Conqueror.Transport.FileSystem/Signalling/FileSystemSignalReceiverRunner.cs`
- `src/transports/file-system/Conqueror.Transport.FileSystem/InboxFiles.cs`
- `src/transports/Conqueror.Transport.ConformityTests/**`

## Transport model overview

Transport selection happens at call sites via fluent builders, not at handler registration. Handlers remain transport-agnostic; transport-specific marker interfaces and attributes make message/signal/iterator types eligible for a transport and feed source generation.

Client side transports implement sender/publisher/client factories. Server side transports implement receiver/server factories and runners. The source-generated type-injector pattern lets transports discover compatible handlers and metadata without runtime reflection.

## HTTP transport details

Messages are exposed as REST endpoints. Defaults are POST, an `api` path prefix, generated path names, optional version segments, 200 for data responses, and 204 for unit responses. GET messages serialize through query strings; other methods use JSON bodies.

ASP.NET Core server integration maps endpoints at startup, attaches endpoint/OpenAPI metadata, rejects duplicate route/method registrations, decodes context headers, and writes upstream context data back to response headers.

Signals can use SSE or WebSockets. WebSockets protocol v1 multiplexes signal tags and context data over binary frames. SSE provides one-way server-to-client streaming.

Iterators use WebSockets for stateful, bidirectional, pull-based streaming. Protocol v1 includes initiation with prefetch count, fetch-more frames, item batches with context data, completion, and error frames.

## File-system transport details

The file-system transport is for local IPC, testing, and development, not production. Messages/signals are identified by tag and optional version. Senders write entries to a store and poll for responses. Receivers poll an inbox and process available entries.

The inbox format uses fixed-width text entries for sequence number, entry ID, tag ID, state, and lease expiration. Single-instance mode processes sequentially without leases. Competing-instance mode uses leasing to avoid concurrent processing and allows retry after failed processing. TTL handling drops expired messages.

## Conformity/invariants

Shared conformity tests require all transports to preserve execution behavior, response/error propagation, trace/message IDs, and downstream/upstream/bidirectional context data. Transport implementations provide host abstractions and are exercised through the same behavioral contracts.

## Design patterns extracted

- Call-site transport selection.
- Marker interfaces and transport attributes.
- Static receiver configuration hooks on handlers.
- Per-transport sender/publisher/client factories and receiver runners.
- A transport conformity test suite.
- Protocol-specific metadata encoded at boundaries, not in business handlers.

## Rust portability assessment

HTTP transport maps well to `axum`, `tower`, `hyper`, `reqwest`, `serde`, `tokio`, WebSocket crates, and `tracing`. SSE and WebSockets are readily available. Iterator protocols can be implemented as explicit WebSocket protocols over `Stream`.

The file-system transport is also feasible with `tokio::fs`, atomic rename/write strategies, locks where available, and a trait-based conformity test harness. It should probably stay a non-production/testing transport.

## Risks and open questions

- ASP.NET endpoint metadata/OpenAPI behavior would need a Rust-specific design.
- Trace propagation needs explicit W3C `traceparent` and context-header conventions.
- WebSocket reconnect, heartbeat, cancellation, and upstream context flow need precise protocol tests.
- File locking/atomic update behavior is OS/filesystem-sensitive.
- Rust should avoid overfitting to ASP.NET abstractions; use a narrower transport trait boundary.
