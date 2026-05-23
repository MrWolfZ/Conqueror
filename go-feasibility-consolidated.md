# Go feasibility report for Conqueror

## 1. Executive answer

**Yes, conditionally.** Conqueror's core ideas can reasonably be reimplemented in Go, but a direct port of the .NET design would be the wrong target.

A Go version can preserve the architectural model: operation-centric APIs, typed request/response handlers, fire-and-forget signals, request/stream operations, middleware, transports, context propagation, and transport conformity tests. Go is perfectly capable of supporting that model.

What Go cannot preserve cleanly is Conqueror's current level of compile-time and fluent API magic. The .NET implementation relies on Roslyn source generators, nested generated interfaces, static abstract interface members, module initializers, `IServiceCollection`, `AsyncLocal`, `Task`, `IAsyncEnumerable<T>`, and source-generated `System.Text.Json` metadata. Go has alternatives, but they are more explicit: structural interfaces, package-level generic functions, generated descriptor code, `context.Context`, manual registries, `net/http`, interceptors, and explicit streaming types.

So the honest answer is:

**A Go-native Conqueror is feasible if it intentionally becomes simpler, more explicit, and less magical than the .NET version. A C#-shaped Conqueror in Go would be awkward, brittle, and not worth building.**

## 2. What Conqueror fundamentally is architecturally

Conqueror is not fundamentally a source generator, a DI extension, or an HTTP abstraction. Those are implementation mechanisms.

Architecturally, Conqueror is a strongly typed application messaging runtime for three operation patterns:

- **Messages**: request/response.
- **Signals**: publish/subscribe or fire-and-forget.
- **Iterators**: request/stream.

Each operation type is meant to be the public application API. Users model business actions as types such as `CreateOrder`, `OrderPlaced`, or `GetLogLines`. Handlers, middleware, dispatch, transport, serialization, registration, and context propagation are infrastructure around those operation types.

The current .NET implementation has a consistent internal shape:

- operation abstractions for messages, signals, and iterators;
- handler abstractions tied to each operation type;
- generated metadata and generated nested handler/pipeline interfaces;
- registries mapping operation types to handlers and transport metadata;
- dispatchers that choose pipeline and transport;
- middleware chains with explicit continuation;
- a context model carrying trace IDs, operation IDs, security principal, and custom data;
- transport adapters for in-process execution, HTTP, WebSockets/SSE, and file-system IPC;
- conformity tests that define required behavior across transports.

The most important product idea is this: **business code should think in application operations, not in controllers, queues, HTTP clients, serializers, or DI plumbing.**

That idea ports to Go. The exact mechanisms do not.

## 3. Patterns that map well to Go

### Operation-centric design

Go can model messages, signals, and streams as plain structs plus registration metadata. This fits Go well. Go programs already tend to prefer simple data structs and small interfaces.

A minimal message handler shape is natural:

```go
type MessageHandler[M any, R any] interface {
    Handle(context.Context, M) (R, error)
}
```

Signals are also straightforward:

```go
type SignalHandler[S any] interface {
    Handle(context.Context, S) error
}
```

The operation type can remain the thing users search for and test against.

### Small interfaces

Go's structural interfaces are a good match for handler contracts. A handler does not need to inherit from framework base classes. If it has the right `Handle` method, it can be registered.

This is a good fit for Conqueror's testing style. Black-box tests can send a message through the runtime instead of directly invoking the handler.

### Explicit constructors and registries

Go does not have a standard DI container, and that is not a problem. Explicit constructors and explicit registration are idiomatic Go. A Conqueror-Go runtime can use a builder/registry without trying to recreate `IServiceCollection`.

For example, the natural Go shape is closer to:

```go
app := conqueror.New()
conqueror.RegisterMessage(app, CreateOrderDescriptor, NewCreateOrderHandler(db))
```

than to assembly scanning or implicit module initialization.

### Middleware chains

Conqueror's middleware model maps cleanly to Go function composition.

A generic message middleware can be expressed as:

```go
type Next[M any, R any] func(context.Context, M) (R, error)
type Middleware[M any, R any] func(Next[M, R]) Next[M, R]
```

Transport middleware maps naturally to `net/http` middleware:

```go
type Middleware func(http.Handler) http.Handler
```

Connect/gRPC interceptors provide a similar concept for RPC transports.

### `context.Context`

Go's `context.Context` is a strong natural fit for cancellation, deadlines, request-scoped values, and trace propagation. Handler signatures should take it as the first argument. That is idiomatic and should not be hidden.

### HTTP transport

Go's HTTP ecosystem is strong enough for Conqueror's transport needs:

- `net/http` for the baseline server/client abstraction;
- Go 1.22+ `ServeMux` for simple routing;
- `chi`, `gin`, or `echo` adapters if users want framework-specific routing;
- `nhooyr.io/websocket`, Gorilla WebSocket, or framework equivalents for WebSockets;
- standard `http.Flusher` patterns for SSE;
- Connect or gRPC if the project chooses a protobuf/RPC-first transport.

The core should not depend on `gin` or `echo`. Standard `net/http` plus optional adapters is the pragmatic default.

### JSON serialization

Go's `encoding/json` works out of the box for plain structs. There is no .NET-style trimming/AOT pressure that forces source-generated serializer contexts.

If performance matters, Go has alternatives such as `jsoniter`, `segmentio/encoding/json`, or generated serializers. Those should be optional, not foundational.

### Transport conformity testing

The existing Conqueror idea of shared behavioral tests for transports maps very well to Go. Go's test package makes this straightforward with reusable test suites that accept a transport factory.

This should be treated as a core design pillar, not an afterthought.

## 4. Patterns that do not map 1:1 and should be redesigned

### Associated response types

C# can attach response type information to a message through generated static interface members and nested generated types. Go has no associated types.

Go can approximate this with generic interfaces such as `Message[R]`, but the ergonomics are not great. External packages also cannot implement an interface with an unexported marker method from the framework package, so the usual sealed-marker trick is not available for user-defined operation types.

The more pragmatic Go design is to use typed descriptors:

```go
type MessageDescriptor[M any, R any] struct {
    Tag     string
    Version string
    Codec   Codec[M, R]
}
```

Then registration and calls carry the descriptor:

```go
conqueror.RegisterMessage(app, CreateOrderMessage, handler)
resp, err := conqueror.Send(ctx, app, CreateOrderMessage, CreateOrder{ProductID: id})
```

This is less magical than `.For(CreateOrder.T).Handle(...)`, but it is honest Go.

### Fluent generic APIs

Go has an important limitation: methods cannot declare their own type parameters. Generic functions and generic types are supported, but a non-generic `App` cannot have a method like this:

```go
// Not valid Go.
func (a *App) Send[M any, R any](ctx context.Context, msg M) (R, error)
```

That makes C#-style fluent APIs harder to reproduce. The Go API should use one of these patterns instead:

- package-level generic functions: `conqueror.Send(ctx, app, descriptor, msg)`;
- generated typed clients/senders;
- generic descriptor/client values with non-generic methods;
- explicit operation-specific helper functions generated by a tool.

Trying to force the C# fluent facade into Go would produce clumsy or untyped APIs.

### Nested generated interfaces

Go has no nested types. There is no equivalent to `CreateOrder.IHandler`.

The Go replacement should be either:

- a generic framework handler interface;
- generated package-level interfaces;
- generated operation-specific helper functions;
- or no generated interface at all, just structural matching during registration.

The simplest option is usually best: accept any value whose method matches the required handler shape.

### Source generators

Go has no Roslyn source generators and no Rust-style proc macros. Code generation exists, but it is external and explicit:

- `go generate`;
- a `conqueror gen ./...` command;
- AST scanning of comments, struct tags, or registration declarations;
- checked-in generated `.go` files.

This is a major difference. `go generate` is not run automatically by `go build` or `go test`. Generated code freshness must be handled through CI checks, tests, or a generator command that developers remember to run.

Code generation should therefore be additive, not required for the first manual core to work.

### Assembly scanning and module initializers

Go has package initialization, but global registration through `init()` is usually a trap for libraries like this. It creates hidden dependencies, import-order surprises, hard-to-test global state, and linker reachability concerns.

Explicit registration should be the default. Generated registration functions are acceptable. Global auto-registration should be avoided or, at most, an optional convenience for small apps.

### `IServiceCollection` and DI lifetimes

Go should not copy .NET DI. It would be a mistake to build a DI framework before building Conqueror.

Use explicit constructors and registries first. If users want DI, provide optional integration patterns for:

- manual constructors;
- Google Wire for compile-time wiring;
- Uber Fx or Dig for runtime DI;
- application-specific composition roots.

The core library should not require any of them.

### Ambient context and upstream propagation

`context.Context` is not a full replacement for `ConquerorContext`.

It works well for:

- cancellation;
- deadlines;
- trace/span identifiers;
- request-scoped values;
- downstream propagation.

It does **not** naturally handle Conqueror's current parent/child context disposal model or upstream propagation of modified context data. Go contexts are immutable by convention, and values should not be abused as mutable bags.

A Go version should probably use both:

- `context.Context` for cancellation/deadlines/tracing; and
- an explicit Conqueror call envelope or metadata object for operation IDs, transport headers, identity, and upstream/bidirectional data.

For example:

```go
type CallContext struct {
    TraceID     string
    OperationID string
    Principal   Principal
    Downstream   Metadata
    Upstream     Metadata
}
```

This can be attached to `context.Context` for convenience, but the protocol model should remain explicit.

### Iterator streaming

Go has several possible streaming idioms, none of which are a perfect `IAsyncEnumerable<T>` equivalent:

- receive-only channels: `<-chan T`;
- callback/yield functions;
- `iter.Seq` / `iter.Seq2` in newer Go versions;
- explicit stream interfaces like `Recv() (T, error)`;
- gRPC/Connect streaming interfaces;
- WebSocket frame protocols.

Channels are tempting but risky. They hide errors badly, can leak goroutines when consumers stop early, and make upstream context propagation awkward. For Conqueror-style request/stream semantics, an explicit stream abstraction is probably better:

```go
type Stream[T any] interface {
    Recv(context.Context) (T, error)
    Close() error
}
```

or, for handlers:

```go
type IteratorHandler[Q any, T any] interface {
    Handle(context.Context, Q) (Stream[T], error)
}
```

The exact design needs care. Streaming is the hardest part of a Go port.

### ASP.NET endpoint metadata and OpenAPI

ASP.NET endpoint metadata does not translate directly. Go has OpenAPI tools, but no single standard comparable to ASP.NET's integrated metadata model.

Options include:

- generator-produced OpenAPI documents;
- comment-based tools such as swaggo;
- schema generation from Go types;
- Connect/gRPC with protobuf descriptors;
- manual route metadata registration.

The honest answer: OpenAPI will be more explicit and probably less seamless unless the Go version commits heavily to code generation.

## 5. Likely Go architecture

A Go-native Conqueror should be a small set of packages with explicit boundaries.

### Module layout

A plausible module layout:

```txt
conqueror/
  app.go                 # App, registry, dispatcher facade
  message.go             # message descriptors, handlers, send API
  signal.go              # signal descriptors, publishers, broadcast strategies
  stream.go              # iterator/stream descriptors and stream API
  context.go             # call metadata helpers around context.Context
  middleware.go          # core pipeline abstractions
  codec.go               # codec interfaces and JSON defaults
  errors.go              # framework error types

conqueror/transport/http/
  server.go              # net/http route registration
  client.go              # HTTP clients
  context_headers.go     # context propagation over HTTP
  sse.go                 # signal streaming if supported
  websocket.go           # iterator/signal websocket protocol if supported

conqueror/transport/connect/   # optional, if Connect/gRPC is chosen
conqueror/transport/filesystem/ # optional local/testing IPC

conqueror/middleware/logging/
conqueror/middleware/auth/
conqueror/middleware/resilience/

conqueror/conformance/
  transport_tests.go     # reusable behavioral test suite

cmd/conqueror-gen/
  main.go                # optional code generation tool
```

The core package should stay independent of router frameworks and DI frameworks.

### Interfaces and generics

Use generics where they improve type safety, but do not fight the language.

Good uses:

```go
type MessageDescriptor[M any, R any] struct { /* ... */ }

type MessageHandler[M any, R any] interface {
    Handle(context.Context, M) (R, error)
}

func RegisterMessage[M any, R any](app *App, desc MessageDescriptor[M, R], h MessageHandler[M, R]) error

func Send[M any, R any](ctx context.Context, app *App, desc MessageDescriptor[M, R], msg M, opts ...CallOption) (R, error)
```

Risky uses:

- deeply nested generic builders;
- generic type erasure hidden behind `any` everywhere;
- global registries keyed only by `reflect.Type`;
- trying to infer response types from message structs without descriptors or generated code.

Internally, the runtime may need some type erasure because one registry stores many message types. That is fine if the public API preserves type safety at registration and send boundaries.

### Code generation

Start without code generation. Add it only to remove repeated descriptor and transport metadata boilerplate.

A generator could support comments like:

```go
//conqueror:message response=CreateOrderResponse tag=orders.create version=v1
//conqueror:http method=POST path=/orders
 type CreateOrder struct {
     ProductID string `json:"productId"`
     Quantity  int    `json:"quantity"`
 }
```

The generated code could provide:

- typed descriptors;
- registration helper functions;
- HTTP route metadata;
- JSON/schema/OpenAPI metadata;
- operation-specific clients or helper functions;
- compile-time assertions that handlers match expected signatures.

But the manual form must remain possible and documented. If the library only works after a generator pass, it will feel fragile in Go.

### Registry and composition

Use explicit app/registry construction.

```go
app := conqueror.New(
    conqueror.WithMiddleware(logging.Middleware(logger)),
)

if err := conqueror.RegisterMessage(app, CreateOrderMessage, NewCreateOrderHandler(repo)); err != nil {
    return err
}
```

Avoid global mutable state. Avoid `init()` registration. Avoid requiring a DI container.

Provide helpers for common composition roots, but keep the core boring.

### Middleware

There are two middleware layers:

1. **Operation middleware** around Conqueror messages/signals/streams.
2. **Transport middleware** around HTTP/RPC endpoints.

Do not blur them.

Operation middleware should see operation metadata, message payload, response/error, transport type, call context, and logger/tracer hooks. Transport middleware should remain normal `net/http` middleware or RPC interceptors.

Logging should likely use `log/slog` or an adapter interface. Tracing should integrate with OpenTelemetry. Authorization should define a small `Principal`/claims model rather than copying `ClaimsPrincipal`.

Resilience can start small:

- timeout;
- retry with backoff;
- circuit breaker later;
- rate limiting later.

Do not promise Polly parity. Go does not have a single Polly-equivalent standard.

### Transports

#### In-process

Build this first. It proves the operation model, registry, handlers, middleware, and context semantics without network complexity.

#### HTTP

Use `net/http` as the core boundary. Generate or register routes into an `http.ServeMux` or any router that can accept `http.Handler`.

Support:

- JSON request/response;
- path/query/body mapping rules;
- response/error mapping;
- context headers;
- tracing headers;
- route conflict detection;
- route metadata for optional OpenAPI generation.

Adapters for `chi`, `gin`, and `echo` can come later if needed.

#### Connect/gRPC

Connect/gRPC is attractive if the Go version wants strong RPC contracts, streaming, generated clients, and multi-language support. But it changes the shape of the library: protobuf becomes the real schema, not plain Go message structs.

This is a strategic choice, not a minor implementation detail. It should be optional unless Conqueror-Go deliberately chooses to become RPC/schema-first.

#### Signals

Start with in-process sequential broadcasting. Then add parallel broadcasting with `errgroup` and explicit error policy.

Remote signals need clear semantics:

- at-most-once vs best-effort;
- per-subscriber errors;
- ordering;
- cancellation;
- backpressure;
- fan-out behavior.

#### Iterators/streams

Treat streaming as a separate design project. Use explicit stream interfaces and protocol tests from day one.

For HTTP, SSE is fine for one-way server-to-client streams. WebSockets or Connect/gRPC streaming are better for bidirectional pull, cancellation, and upstream metadata. If Conqueror's iterator semantics require pull-based fetch-more and bidirectional context, WebSockets or RPC streaming are the realistic options.

#### File-system transport

Feasible, but keep it clearly positioned as local IPC/testing/dev tooling. File locks, atomic writes, leases, TTLs, retries, and cross-platform behavior are easy to get subtly wrong. Go can implement it, but this should not be a flagship production transport.

### Context model

Use `context.Context` in every public operation:

```go
resp, err := conqueror.Send(ctx, app, CreateOrderMessage, req)
```

Define explicit metadata helpers:

```go
ctx = conqueror.WithTrace(ctx, traceID)
ctx = conqueror.WithPrincipal(ctx, principal)
meta := conqueror.MetadataFromContext(ctx)
```

But do not turn `context.Context` into a mutable key/value dumping ground. For upstream/bidirectional data, use explicit result/envelope mechanics or a dedicated operation context object.

## 6. API ergonomics comparison

### What Go can preserve

Go can preserve these ergonomic wins:

- operation-centric design;
- plain data structs as messages/signals/stream requests;
- strong handler signatures;
- black-box testing through send/publish/stream APIs;
- middleware separated from business handlers;
- transport-agnostic handlers;
- call options for choosing transport/pipeline behavior;
- explicit context and cancellation;
- simple HTTP exposure for common cases;
- generated helpers for reducing repetitive descriptors.

A reasonable Go call site could be:

```go
resp, err := conqueror.Send(ctx, app, messages.CreateOrder, messages.CreateOrderRequest{
    ProductID: productID,
    Quantity:  quantity,
})
```

or, with generated helpers:

```go
resp, err := messages.SendCreateOrder(ctx, app, messages.CreateOrderRequest{
    ProductID: productID,
    Quantity:  quantity,
})
```

That is not identical to C#, but it is idiomatic enough.

### What will be worse

Go will be worse than .NET Conqueror in several areas:

- no `CreateOrder.IHandler` nested handler discoverability;
- weaker type-level association between request and response;
- less powerful fluent generic APIs;
- no automatic source generation during normal build;
- no assembly scanning equivalent;
- no standard DI container;
- less integrated OpenAPI metadata story;
- less ability to hide registry and transport glue without code generation;
- more manual registration unless generated helpers are used.

The biggest ergonomic loss is that Go cannot easily make the operation type itself carry all the compile-time metadata and response association in the same elegant way C# can with generated static members.

### What could be better

Go could be better in other ways:

- simpler handler signatures;
- fewer framework concepts for users to learn;
- less hidden DI behavior;
- standard cancellation and deadlines through `context.Context`;
- easy use of standard `net/http` tooling;
- explicit composition that is easier to debug;
- straightforward black-box tests with no test host if using in-process transport;
- fewer runtime reflection/AOT concerns than .NET;
- smaller conceptual surface if the Go version resists over-engineering.

Go's simplicity is a double-edged sword. It helps the runtime be understandable and testable. It works against Conqueror's current ergonomic goal of hiding almost all infrastructure behind generated fluent APIs.

The pragmatic answer is to embrace Go's explicitness instead of pretending it is C#.

## 7. Risk assessment

### Technical risks

#### Generic API dead ends

Go generics are useful but intentionally limited. The absence of associated types and generic methods can easily lead to awkward APIs. If the design starts by chasing `.For(T).WithPipeline(...).WithTransport(...).Handle(...)`, it will probably fail.

Mitigation: design around descriptors, package-level generic functions, and generated helper functions.

#### Registry type erasure

A runtime registry must store handlers for many concrete message types. Internally that means some type erasure through `any`, reflection, or generated dispatch functions.

Mitigation: keep type erasure internal and validate type safety at registration boundaries.

#### Code generation freshness

`go generate` is not automatic. Generated descriptors and route metadata can go stale.

Mitigation: make manual descriptors valid, keep generation optional at first, and add CI checks for generated-code drift if generation becomes important.

#### Context misuse

It is very easy in Go to abuse `context.Context` as a mutable data bag. That would be unidiomatic and error-prone.

Mitigation: use context for cancellation/deadlines/request values, and design explicit metadata/envelope types for Conqueror-specific propagation.

#### Streaming leaks

Channel-based APIs can leak goroutines when consumers stop early. They also handle errors and backpressure poorly unless designed carefully.

Mitigation: use explicit stream interfaces with `Recv(ctx)`, `Close`, clear error semantics, and conformance tests for cancellation/backpressure.

#### HTTP/schema mismatch

Plain Go structs plus `encoding/json` do not automatically produce high-quality OpenAPI schemas or route metadata.

Mitigation: treat OpenAPI as a generator feature or choose Connect/protobuf for schema-first use cases.

#### Framework lock-in

Choosing `gin`, `echo`, or a specific router as the core HTTP abstraction would unnecessarily narrow adoption.

Mitigation: core HTTP transport should be `net/http`; framework adapters should be optional.

#### Resilience parity

Polly parity is unrealistic as an initial goal. Go has retries, timeouts, rate limiters, and circuit breaker libraries, but no single universal equivalent.

Mitigation: implement a small set of well-tested middleware and leave advanced policy composition for later.

#### File-system transport correctness

Leases, TTL, polling, atomic writes, and cross-platform filesystem behavior are trap-heavy.

Mitigation: make it optional and test it hard. Do not present it as production-grade without proof.

### Complexity hotspots

The hardest areas will be:

1. preserving type safety while storing heterogeneous handlers;
2. designing a pleasant API despite Go's generic method limitations;
3. context propagation with upstream/bidirectional metadata;
4. streaming protocols and cancellation;
5. OpenAPI/schema generation;
6. deciding how much code generation is acceptable;
7. keeping transport adapters conformant without overfitting to one framework.

### Likely traps

- Building a DI container because .NET has one.
- Using global `init()` registration to mimic module initializers.
- Hiding everything behind reflection and losing compile-time guarantees.
- Overusing `context.WithValue` for framework state.
- Making channels the only streaming abstraction.
- Letting generated code become required before the manual model is stable.
- Choosing `gin`/`echo` as the core instead of `net/http`.
- Promising OpenAPI quality without a generator strategy.
- Copying the Rust feasibility conclusion and ignoring that Go has weaker type-level tools but simpler runtime composition.

## 8. Recommended implementation strategy/phases

### Phase 0: Define the Go-native goal

State explicitly that the goal is conceptual parity, not source/API parity with .NET.

Acceptance criteria for the first milestone should be boring:

- define a message struct;
- register a handler explicitly;
- send the message in-process;
- run middleware;
- propagate cancellation and operation metadata;
- test through the public send API.

No generator. No HTTP. No DI. No streams.

### Phase 1: Build the smallest in-process message runtime

Implement:

- `App` / registry;
- `MessageDescriptor[M,R]`;
- `MessageHandler[M,R]`;
- `RegisterMessage`;
- `Send`;
- basic framework errors;
- explicit call metadata;
- black-box tests.

This phase proves whether the descriptor/generic function API is acceptable.

### Phase 2: Add operation middleware

Implement a generic middleware chain for messages.

Test:

- ordering;
- short-circuiting;
- error propagation;
- panic-to-error policy if any;
- context cancellation;
- metadata visibility.

Add minimal logging and authorization only after the generic chain works.

### Phase 3: Define context propagation precisely

Before adding remote transports, specify:

- trace ID behavior;
- operation ID behavior;
- downstream metadata;
- upstream metadata;
- bidirectional metadata;
- identity/principal model;
- error propagation format.

This must be done before HTTP/WebSockets or it will be retrofitted badly.

### Phase 4: Add code generation for descriptors and helpers

Add `cmd/conqueror-gen` only after the manual API is stable.

Generate:

- descriptors;
- operation-specific send/publish helpers;
- registration helpers;
- route metadata;
- optional OpenAPI/schema metadata.

Keep generated code readable and checked in. Add a CI check for drift if the repository relies on generation.

### Phase 5: Add HTTP message transport

Implement request/response messages over `net/http`.

Test:

- route registration;
- duplicate routes;
- JSON body/query/path mapping;
- status codes;
- framework error format;
- context header propagation;
- cancellation and timeout behavior;
- transport conformity against in-process behavior.

Do not add WebSockets or signals in this phase.

### Phase 6: Add signals

Implement in-process signals first:

- sequential broadcast;
- explicit error policy;
- optional parallel broadcast later using `errgroup`;
- middleware interaction;
- tests for ordering and cancellation.

Remote signals can follow once semantics are nailed down.

### Phase 7: Add streaming/iterators

Design streams separately. Do not treat channels as an automatic answer.

Implement:

- explicit stream interface;
- in-process stream handling;
- cancellation tests;
- backpressure tests;
- close/error tests;
- context metadata per item if required.

Only then add WebSocket or Connect/gRPC streaming.

### Phase 8: Add optional advanced transports and middleware

After the core is stable:

- Connect/gRPC transport if schema-first RPC is desired;
- SSE/WebSocket signal transport;
- file-system transport for local/testing IPC;
- OpenAPI generation;
- resilience middleware;
- framework adapters for `chi`, `gin`, `echo`;
- optional Wire/Fx/Dig integration examples.

## 9. Clear final recommendation

Build a Go reimplementation only if the goal is **Go-native conceptual parity**: typed application operations, explicit registration, middleware, context propagation, and transports with conformity tests.

Do **not** build it if the goal is to reproduce the .NET API shape. Go will not give you nested generated handler interfaces, associated response types, automatic source generators, assembly scanning, module-initializer registration, or the same fluent generic call-site ergonomics. Forcing those ideas into Go would create a worse library than both the .NET original and an idiomatic Go design.

The recommended Go design is:

1. plain structs for operation payloads;
2. typed descriptors to bind request/response/metadata;
3. package-level generic functions or generated typed helpers for calls;
4. explicit registration and explicit constructors;
5. `context.Context` everywhere, with a separate metadata model for Conqueror-specific propagation;
6. operation middleware as simple function composition;
7. `net/http` as the base HTTP transport, with optional router adapters;
8. explicit stream abstractions instead of naive channel-only APIs;
9. optional code generation after the manual API is proven;
10. transport conformity tests as a non-negotiable engineering practice.

In short: **yes, Go can support a Conqueror-like library, but Go's simplicity must become a design constraint, not an obstacle to be hidden. A successful Go version would be less magical, more explicit, somewhat less ergonomic at declaration/call sites, and likely easier to debug and operate.**
