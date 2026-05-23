# Rust feasibility report for Conqueror

## 1. Executive answer

**Yes, conditionally.** Conqueror's architectural ideas can reasonably be reimplemented in Rust, but a direct port is the wrong target.

A Rust version can preserve the core concepts: message-centric APIs, strongly typed handlers, middleware pipelines, call-site transport selection, context propagation, transport conformity tests, and generated compile-time metadata. That is conceptual parity, and it is feasible.

Direct API parity is not realistic. The current C# design leans hard on Roslyn source generators, nested generated interfaces, static abstract interface members, `IServiceCollection`, module initializers, `AsyncLocal`, `Task`, `IAsyncEnumerable`, and `System.Text.Json` source-generation. Rust has alternatives, but they produce a different shape: traits, associated types, derive/proc macros, explicit registration, `tower`-style middleware, `serde`, `tokio`, `Stream`, and more visible composition.

So the honest answer is: **a Rust-native Conqueror is viable; a C#-shaped Conqueror in Rust would be painful, overly magical, and probably not worth building.**

## 2. What Conqueror fundamentally is

Architecturally, Conqueror is not primarily a transport library, a DI helper, or a source-generator trick. It is a strongly typed application messaging runtime for three communication patterns:

- **Messages**: request/response.
- **Signals**: publish/subscribe / fire-and-forget.
- **Iterators**: request/stream.

Each pattern has the same broad structure:

- a user-defined operation type;
- a handler abstraction;
- a generated type contract and metadata;
- a runtime registry;
- a dispatcher;
- an optional middleware pipeline;
- a transport boundary;
- context propagation across in-process and remote calls.

The important design move is that **the operation type is the public API**. Users search for and call `CreateOrder`, `OrderPlaced`, or `GetLogLines`, not transport clients or controller methods. Handler implementation, middleware, context, and transport plumbing sit behind a fluent facade.

The current .NET implementation combines compile-time generated metadata with runtime DI composition. That allows ergonomic APIs while avoiding late reflection where AOT/trimming would hurt.

## 3. Patterns that map well to Rust

These concepts are a good fit for Rust, provided they are expressed idiomatically:

- **Message/signal/iterator traits**: `Message { type Response }`, `Signal`, and `IteratorRequest { type Item }` map cleanly to Rust traits with associated types and constants.
- **Strong handler pairing**: typed handler traits can encode that a handler handles exactly one operation type.
- **Compile-time metadata**: derive/proc macros can generate tags, versions, serializer metadata, schema data, and type tokens.
- **AOT/no-reflection posture**: Rust naturally has no runtime reflection dependency, so explicit generated metadata is a reasonable design.
- **Middleware pipelines**: Conqueror's chain-of-responsibility model maps well to `tower::Service` and `tower::Layer`, or a small custom equivalent.
- **Transport abstraction**: HTTP maps well to `axum`/`hyper`/`reqwest`; WebSockets/SSE are available; file-system IPC is feasible with `tokio::fs` and careful atomicity rules.
- **Async streaming**: iterator handlers can return `Stream<Item = Result<T, E>>` or boxed streams.
- **Transport conformity tests**: the existing invariant-driven transport testing model is very Rust-friendly and should be preserved.
- **Explicit context object**: trace IDs, operation IDs, headers, identity, and extension data can be represented cleanly in a request context struct.

## 4. Patterns that should be redesigned, not copied

The following .NET patterns do not map 1:1 and should not be forced into Rust:

- **Nested generated handler interfaces**: Rust cannot naturally generate C#-style nested interfaces inside a user type. Use generated modules, traits, or plain handler traits instead.
- **Static abstract interface members**: use associated constants, associated types, generated impls, and marker traits.
- **Module initializer registration**: Rust can use `inventory`/`linkme`, but explicit registration is usually clearer and more predictable. Automatic global registration should be optional, not foundational.
- **Assembly scanning**: Rust has no assembly equivalent. Crate/module-level registration functions are a better default.
- **`IServiceCollection` as the center of the world**: Rust has no standard DI container. Use explicit builders and owned registries; optionally integrate with DI-like crates later.
- **`AsyncLocal`-heavy ambient context**: task-local state exists, but Rust will be cleaner if context is passed explicitly through the request/service value. Use task-local storage sparingly.
- **`ClaimsPrincipal`**: define a small domain-owned identity/claims model instead of copying .NET security abstractions.
- **`IAsyncEnumerable` iterator semantics**: model streams explicitly with `Stream`; design cancellation, backpressure, prefetch, and upstream context flow deliberately.
- **Attribute-driven HTTP behavior copied from ASP.NET**: Rust should define route generation around `axum`/`tower` concepts, not ASP.NET endpoint metadata.

## 5. Likely Rust architecture

A plausible Rust-native layout:

- `conqueror-core`
  - operation traits: `Message`, `Signal`, `IteratorRequest`;
  - handler traits;
  - context model;
  - dispatcher/facade APIs;
  - registry traits and in-process transport;
  - error types and cancellation conventions.
- `conqueror-macros`
  - `#[derive(Message)]`, `#[derive(Signal)]`, `#[derive(IteratorRequest)]`;
  - optional attributes for tag, version, response/item type, HTTP metadata;
  - generated metadata impls.
- `conqueror-middleware-*`
  - logging via `tracing`;
  - authorization via policy traits;
  - resilience via `tower` timeout/retry layers plus selected crates or custom layers.
- `conqueror-transport-http`
  - server integration for `axum` or a lower-level `tower` abstraction;
  - client integration over `reqwest`;
  - SSE/WebSocket support for signals and iterators.
- `conqueror-transport-file-system`
  - testing/local IPC transport;
  - explicit warning that it is not production-grade unless proven otherwise.
- `conqueror-conformity-tests`
  - shared behavioral tests for transports: response/error propagation, trace/operation IDs, downstream/upstream/bidirectional context, streaming completion/cancellation.

Core trait shape could be roughly:

```rust
trait Message: Send + Sync + 'static {
    type Response: Send + Sync + 'static;
    const TAG: &'static str;
}

#[async_trait::async_trait]
trait MessageHandler<M: Message>: Send + Sync + 'static {
    async fn handle(&self, message: M, ctx: Context) -> Result<M::Response, Error>;
}
```

That is not final API design, but it shows the likely direction: associated types, explicit context, and async trait ergonomics. For performance-sensitive internals, boxed futures can be replaced with generic services where worth the complexity.

Registration should start explicit:

```rust
let app = Conqueror::builder()
    .add_message_handler::<CreateOrderHandler, CreateOrder>()
    .layer(logging())
    .transport(http())
    .build();
```

Macro-assisted shortcuts can come later, after the explicit model is proven.

## 6. API ergonomics comparison

### What Rust can preserve

- Message-centric design: the operation type remains the user's API surface.
- Strong request/response typing.
- Separation between handler logic, middleware, and transport.
- Call-site selection of pipeline/transport, if the facade is designed carefully.
- Low-boilerplate serialization through `serde` derives.
- Black-box tests that send messages rather than directly invoking handlers.
- Transport conformance tests as a first-class contract.

### What will likely be worse

- Less implicit discovery. Rust users will probably need explicit registration or module registration functions.
- More visible generics, trait bounds, lifetimes, and async boxing decisions.
- Proc macro diagnostics will be worse than normal compiler errors and worse than well-tested Roslyn generator diagnostics.
- No clean equivalent to `GetCounterValue.IHandler` nested under the message type.
- No standard DI abstraction means examples may feel more framework-specific or more manual.
- HTTP/OpenAPI customization will need more explicit design than ASP.NET attributes.

### What could be better

- The no-reflection/AOT story is more natural in Rust.
- Ownership and type checking can prevent categories of runtime errors that C# only catches through tests or DI validation.
- `tower` can provide a cleaner middleware foundation than a bespoke pipeline if the public API hides its complexity.
- Context can be made explicit and easier to reason about than ambient `AsyncLocal` propagation.
- Transport protocol code can be very precise about backpressure, cancellation, and stream ownership.

## 7. Risk assessment

### Main technical risks

- **Proc macro overreach**: trying to recreate the C# magic will create a fragile macro-heavy library. Keep macros small and additive.
- **Async trait/object-safety complexity**: handlers, middleware, and transport registries will force choices between `async-trait`, boxed futures, dynamic dispatch, and generic explosion.
- **Registration design**: automatic registration is tempting but can become linker/platform magic. Explicit registration is less magical and easier to debug.
- **Context propagation**: downstream/upstream/bidirectional context must be specified precisely, especially across HTTP headers and WebSocket frames.
- **Iterator protocol complexity**: request/stream is the hardest pattern. Prefetch, fetch-more, cancellation, errors, reconnects, heartbeats, and upstream context need protocol tests from day one.
- **Resilience parity**: Polly is broad. Rust can cover timeout/retry/rate-limit/circuit-breaker behavior, but exact parity is a project on its own.
- **File-system transport correctness**: leases, TTLs, atomic writes, locking, and cross-platform filesystem semantics are trap-heavy.
- **OpenAPI/server metadata**: ASP.NET endpoint metadata does not translate directly. Treat this as a new design.

### Likely traps

- Building a Rust DI framework before building Conqueror.
- Treating `inventory`/`linkme` as the default because it feels like module initializers.
- Hiding too much behind macros before the manual API is stable.
- Letting `tower` types leak into every user-facing signature.
- Claiming parity with C# middleware/resilience/HTTP metadata before writing conformance tests.
- Porting the file-system transport as if it were production infrastructure.

## 8. Recommended strategy

### Phase 0: Decide scope honestly

State up front that the goal is **Rust-native conceptual parity**, not source/API parity with .NET. If exact C# ergonomics are required, stop; Rust is the wrong platform for that requirement.

### Phase 1: Build the smallest in-process message core

Implement only messages first:

- `Message` trait;
- `MessageHandler` trait;
- explicit registry/builder;
- in-process dispatcher;
- explicit `Context`;
- black-box send tests.

No HTTP, no signals, no iterators, no automatic registration, no clever macros beyond maybe a simple derive.

### Phase 2: Add middleware

Introduce a pipeline model, preferably internally compatible with `tower` concepts. Add logging and a minimal authorization middleware. Prove middleware ordering, short-circuiting, errors, and context behavior with tests.

### Phase 3: Add macros for ergonomics

Only after the manual model works, add derives/attributes to reduce boilerplate. The macro should generate metadata and simple impls, not hide architectural decisions.

### Phase 4: Add signals

Signals are simpler than iterators but expose broadcast semantics and error policy decisions. Implement sequential broadcasting first; add parallel broadcasting only when the semantics are clear.

### Phase 5: Add HTTP messages

Implement message request/response over HTTP. Define route metadata, request encoding, response/error mapping, and context headers. Add transport conformity tests immediately.

### Phase 6: Add iterators/streaming

Design this deliberately around Rust streams and WebSockets. Do not mechanically port the .NET protocol unless compatibility with .NET clients is a hard requirement. Write protocol tests for cancellation, backpressure, completion, errors, and context propagation.

### Phase 7: Optional transports and advanced middleware

Add file-system transport, OpenAPI generation, richer resilience, and automatic registration only after the core model has proven itself.

## 9. Final recommendation

Build a Rust reimplementation only if the goal is to support Rust applications with the same architectural model as Conqueror. That is a reasonable project.

Do **not** attempt a direct port. The current .NET design is deeply shaped by C# and the .NET runtime. Copying it literally would produce an awkward Rust library with too much macro magic, too much hidden registration, and too many non-idiomatic abstractions.

The pragmatic recommendation is:

1. keep the Rust design message-centric and transport-agnostic;
2. make registration explicit first;
3. use small proc macros for metadata and ergonomics;
4. use explicit context rather than ambient context as the default;
5. build on `tower`, `tokio`, `serde`, `tracing`, and an HTTP stack such as `axum`/`hyper`/`reqwest`;
6. preserve Conqueror's strongest engineering practice: transport conformity tests.

In short: **conceptual parity is feasible and worthwhile; direct API parity is not.**
