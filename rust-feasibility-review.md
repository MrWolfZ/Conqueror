# Rust feasibility proposal review

## Scope

This review challenges `rust-feasibility-consolidated.md` against Conqueror's local architecture notes and the Rust ecosystem. It intentionally leaves the original proposal unchanged.

Local references reviewed:

- `rust-feasibility-consolidated.md`
- `core-architecture.md`
- `transports.md`
- `source-generators.md`
- `middlewares.md`
- `usage-ergonomics.md`
- `README.md`

External references checked include Tower, Hyper, Axum, Tonic, Tarpc, Actix, Ractor, `mediator`, `mediator-rs`, event-bus crates, NATS, `tracing`, OpenTelemetry, `inventory`, and `linkme`.

## Executive verdict

The proposal is directionally correct, but not hard enough on its own scope.

A Rust Conqueror is feasible, but the right product is not a from-scratch messaging runtime. The right product is a thin, typed coordination layer over the Rust service ecosystem:

- Tower for service and middleware composition.
- Axum for HTTP routing and server integration.
- Tonic for optional gRPC transport.
- Tokio and `Stream` for async execution and streaming.
- Serde for payload serialization.
- `tracing` and OpenTelemetry for trace propagation.
- Small proc macros for operation metadata.
- Conformance tests for every transport.

Conqueror's unique value is not HTTP, RPC, middleware, pub/sub channels, or async streams. Rust already has those. Conqueror's value would be the operation-centric API, call-site transport selection, transport-agnostic middleware, context-flow semantics, compile-time operation metadata, and shared transport conformity tests.

## The proposal gets these things right

1. **Direct API parity with .NET is the wrong target.** Rust should preserve concepts, not C# shapes such as nested generated interfaces, `IServiceCollection`, `AsyncLocal`, and module initializers.
2. **Explicit registration should come first.** Link-time registration through `inventory` or `linkme` is real, but it should be optional because it introduces linker/platform magic and debugging friction.
3. **AOT/no-reflection maps naturally to Rust.** Associated types, constants, derives, and explicit metadata are a good Rust substitute for source-generator-owned static metadata.
4. **Middleware should align with Tower.** The proposal says this, but it should be stronger.
5. **Iterator/request-stream semantics are the hardest part.** The proposal correctly warns against blindly porting the .NET protocol.
6. **Transport conformity tests are worth preserving.** This is one of Conqueror's strongest ideas and is more distinctive than most of the runtime implementation.

## Main challenges

### 1. Tower must be a foundation, not inspiration

The proposal says middleware should be "internally compatible with `tower` concepts." That is too weak.

Tower's `Service<Request>` and `Layer<Service>` are already the Rust ecosystem's standard abstraction for protocol-agnostic request/response handling and middleware composition. Axum and Tonic both build on it. If Rust Conqueror defines a parallel middleware abstraction, it risks becoming an island and reimplementing existing middleware patterns.

The design should start from one of these positions:

1. Conqueror handlers are Tower services directly.
2. Conqueror handlers adapt into Tower services at the registry boundary.
3. Conqueror deliberately does not use Tower, with a concrete reason strong enough to justify losing ecosystem interoperability.

Option 2 is probably the best default: keep the public API operation-centric, but make the runtime pipeline Tower-backed.

### 2. Hyper is not the right analogue

Hyper is a low-level HTTP implementation. It is important, but it is not similar to Conqueror's design.

The better mapping is:

| Rust crate | Role |
|---|---|
| Hyper | Low-level HTTP engine |
| Tower | Service/middleware abstraction |
| Axum | HTTP routing, extractors, responses, SSE/WebSocket integration |
| Tonic | gRPC transport, generated typed RPC, streaming |

Conqueror should not build directly on Hyper unless it has a specific low-level protocol reason. For normal HTTP transport, Axum is the right target. Hyper should usually remain a transitive foundation through Axum or Tonic.

### 3. Tarpc is a serious missing comparison

Tarpc is a code-first typed RPC framework. It already has:

- generated client/server stubs from Rust service definitions;
- pluggable transports;
- request context;
- deadline propagation;
- cascading cancellation;
- distributed tracing support.

That overlaps heavily with Conqueror's message/request-response story.

Tarpc does not replace Conqueror because it does not provide the same three-pattern model, per-operation middleware pipeline, HTTP-native route generation, call-site transport selection, or transport conformance suite. But the proposal should explicitly say that. Ignoring Tarpc makes the proposal look under-researched.

### 4. The `mediator` crates are unacknowledged overlap

The `mediator` crate is explicitly inspired by MediatR and already models:

- request/response;
- events;
- stream requests;
- sync and async mediator variants.

`mediator-rs` also models CQRS-style request dispatch with pipeline behaviors.

These crates are not complete Conqueror substitutes. They lack Conqueror's transport abstraction, context model, generated metadata, HTTP integration, and transport conformity tests. Still, they prove that a basic Rust mediator is not novel. A Rust Conqueror proposal should position itself as more than "MediatR in Rust."

### 5. Tonic should be considered for a gRPC transport

The proposal talks about HTTP but does not seriously evaluate gRPC.

Tonic already provides typed request/response, server streaming, client streaming, bidirectional streaming, Tower middleware integration, HTTP/2 transport, metadata, and generated clients/servers. A future `conqueror-transport-grpc` should probably wrap or generate Tonic services rather than inventing a custom RPC stack.

This does not mean the MVP needs gRPC. It means the architecture should avoid choices that make a Tonic transport awkward later.

### 6. Context propagation should not duplicate tracing

Conqueror's context has two separable responsibilities:

1. trace identity and distributed tracing;
2. application-specific transportable data with downstream, upstream, or bidirectional flow.

Rust already has strong tracing infrastructure. Trace propagation should lean on `tracing`, `tracing-opentelemetry`, and `opentelemetry-http` rather than inventing custom trace header plumbing.

Conqueror should own the second responsibility: typed or string-keyed application context data, flow direction, operation IDs, and transport conformance rules. It should not pretend to be an observability stack.

### 7. Signals are not just broadcast channels

`tokio::sync::broadcast` is useful infrastructure, but it does not by itself match Conqueror signalling semantics.

Conqueror's signal dispatch can involve handler discovery, per-handler middleware, broadcasting strategy, error policy, context propagation, and transport conformity. A broadcast channel is a building block for fan-out, not a replacement for the dispatcher.

The proposal should separate:

- local in-process fan-out mechanics;
- publisher pipeline behavior;
- observer/handler pipeline behavior;
- completion/error semantics;
- remote signal transport semantics.

### 8. SSE is not equivalent to Conqueror iterators

SSE is useful for one-way server-to-client streaming, especially signals or push streams. It is not equivalent to Conqueror's iterator model if the important property is pull-based consumption where each move-next can carry context upstream.

For iterator transports, the realistic candidates are:

- WebSockets for explicit bidirectional protocol frames;
- gRPC server streaming where upstream context is not required after initiation;
- gRPC bidirectional streaming if per-item upstream client frames are required;
- SSE only for simplified one-way streaming cases.

The proposal is right to treat iterators as a later phase. It should be even more explicit that iterator protocol semantics are a design problem, not an implementation detail.

## Ecosystem cross-check

| Existing solution | Overlap with Conqueror | Difference / implication |
|---|---|---|
| Tower | Middleware, async request/response services, protocol-agnostic composition | Should be the runtime pipeline substrate or adapter target. Do not clone it casually. |
| tower-http | HTTP-specific middleware such as tracing, compression, CORS, request IDs, body limits | Reuse where HTTP-specific behavior is enough; keep operation-level middleware separate. |
| Axum | HTTP routing, extractors, responses, state, SSE/WebSockets via ecosystem | Best HTTP server integration target. Generate/build Axum routers from operation metadata. |
| Hyper | Low-level HTTP/1 and HTTP/2 | Too low-level as the main Conqueror analogue. Use indirectly through Axum/Tonic. |
| Reqwest | High-level HTTP client | Good client substrate for JSON/HTTP transport. Not an application messaging abstraction. |
| Tonic | gRPC, typed generated stubs, streaming, Tower integration | Strong candidate for a gRPC transport. Do not invent custom gRPC-like behavior. |
| Tarpc | Code-first typed RPC, pluggable transport, context, deadlines, cancellation, tracing | Closest request/response competitor. Conqueror must differentiate clearly. |
| Actix | Typed actor messages and handlers | Confirms typed message/result traits are idiomatic; actor runtime is the wrong center for Conqueror. |
| Ractor | Actor messaging and supervision | Same as Actix: useful reference, not the right foundation. |
| `mediator` | Requests, events, stream requests | Biggest in-process mediator overlap; lacks transport/context/metadata/conformance. |
| `mediator-rs` | CQRS dispatch and pipeline behaviors | Similar core idea; limited ecosystem maturity and no transport story. |
| `tokio::sync::broadcast` | Typed local fan-out | Useful signal primitive, not a full signal dispatcher. |
| NATS / async-nats | Durable/cloud-native pub/sub and request/reply messaging | Candidate remote signal/message transport, not a replacement for operation-centric API. |
| `inventory` / `linkme` | Link-time distributed registration | Optional ergonomic layer only; explicit registration should remain primary. |
| `tracing` / OpenTelemetry | Structured logging, spans, distributed trace propagation | Should carry trace context; Conqueror should own application context flow semantics. |
| `poem-openapi` | Proc-macro-driven OpenAPI generation | Useful design reference if Conqueror later generates OpenAPI from operation metadata. |

## Revised architectural recommendation

The proposal should be rewritten around this statement:

> Rust Conqueror is a typed operation coordination layer that composes Tower, Axum, Tokio, Serde, Tracing, and optionally Tonic. It adds operation metadata, call-site transport selection, context-flow semantics, and conformance-tested transports. It does not reimplement middleware, HTTP, RPC, async streams, or observability infrastructure.

That framing is narrower and stronger than the current proposal.

## Recommended MVP

The MVP should prove only the message/request-response path.

In scope:

1. `Message` trait with associated `Response`.
2. `Handler<M>` public trait or helper API that adapts to a Tower `Service`.
3. Explicit registry/builder.
4. In-process dispatcher.
5. Explicit context object with operation ID and application context data.
6. Tower-backed middleware pipeline.
7. Minimal derive macro for tag/version metadata, if manual metadata is too noisy.
8. Black-box tests that dispatch messages through the public facade.

Out of scope:

1. Signals.
2. Iterators.
3. HTTP transport.
4. gRPC transport.
5. file-system transport.
6. automatic registration.
7. OpenAPI generation.
8. advanced resilience middleware.
9. framework-specific DI.

This is intentionally smaller than the proposal. Anything bigger risks proving the wrong thing.

## Design decisions to make before implementation

1. **Is the internal pipeline Tower-first?** If not, why not?
2. **Is the public handler trait a Tower service, an adapter to Tower, or independent?**
3. **What exactly is the Rust equivalent of `ConquerorContext`?**
4. **Which context fields are trace/observability and which are application-level data?**
5. **What is the error model?** One Conqueror error type, handler-specific errors, transport errors, or layered conversion?
6. **How does call-site transport selection look without recreating .NET DI?**
7. **Will handler registration be explicit forever, or is optional link-time registration planned?**
8. **How will the design differentiate from Tarpc and `mediator` in public documentation?**

## Final recommendation

Do not build a Rust port yet.

First, revise the proposal to make ecosystem composition mandatory, not aspirational. Then build the smallest in-process message prototype with Tower-backed middleware. If that prototype cannot preserve Conqueror's operation-centric ergonomics without ugly trait/lifetime complexity, the project should stop before adding transports, signals, iterators, or macros.

If the prototype works, the next phase should be HTTP messages with Axum and transport conformance tests. Signals and iterators should wait until the message model is proven.

## Reference links

- Tower: https://docs.rs/tower/latest/tower/
- tower-http: https://docs.rs/tower-http/latest/tower_http/
- Axum: https://docs.rs/axum/latest/axum/
- Hyper: https://hyper.rs/
- Tonic: https://docs.rs/tonic/latest/tonic/
- Tarpc: https://docs.rs/tarpc/latest/tarpc/
- Actix: https://docs.rs/actix/latest/actix/
- Ractor: https://docs.rs/ractor/latest/ractor/
- mediator: https://docs.rs/mediator/latest/mediator/
- mediator-rs: https://docs.rs/mediator-rs/latest/mediator_rs/
- async-nats: https://docs.rs/async-nats/latest/async_nats/
- tracing: https://docs.rs/tracing/latest/tracing/
- opentelemetry-http: https://docs.rs/opentelemetry-http/latest/opentelemetry_http/
- inventory: https://docs.rs/inventory/latest/inventory/
- linkme: https://docs.rs/linkme/latest/linkme/
- poem-openapi: https://docs.rs/poem-openapi/latest/poem_openapi/
