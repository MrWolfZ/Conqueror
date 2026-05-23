# Rust prototype MVP plan

## Goal

Build the smallest Rust prototype that proves Conqueror's core message-centric model can work idiomatically in Rust.

The prototype is not a port of the .NET API. It is a feasibility spike for one question:

> Can Rust provide a discoverable, strongly typed, operation-centric request/response API with explicit registration, explicit context, and Tower-backed middleware without becoming trait/lifetime soup?

If the answer is no, the Rust effort should stop before adding transports, signals, iterators, macros, or OpenAPI.

## Non-goals

The MVP must not include:

1. HTTP transport.
2. gRPC transport.
3. signals/pub-sub.
4. iterators/request-stream.
5. file-system transport.
6. automatic registration through `inventory` or `linkme`.
7. OpenAPI generation.
8. a DI framework.
9. production-grade resilience middleware.
10. compatibility with the .NET wire protocols.

These are deliberately excluded. Adding them before the in-process message core is proven would hide the real feasibility risk.

## Design position

The prototype should use a public Conqueror-shaped API and a Tower-shaped runtime.

Recommended stance:

- Users implement a small `MessageHandler<M>` trait.
- The registry adapts each handler into a Tower `Service<MessageRequest<M>>`.
- Middleware is applied as Tower `Layer`s before the typed service is erased into the heterogeneous registry.
- Dispatch remains operation-centric: callers send `M` and receive `M::Response`.

This avoids forcing users to implement Tower directly while still proving that Conqueror can compose with Tower instead of cloning it.

## Prototype crate layout

Use a separate Rust workspace so the spike does not interfere with the .NET solution.

Suggested path:

```txt
experiments/rust-conqueror/
├── Cargo.toml
├── conqueror-core/
│   ├── Cargo.toml
│   └── src/
│       ├── lib.rs
│       ├── context.rs
│       ├── dispatch.rs
│       ├── error.rs
│       ├── handler.rs
│       ├── message.rs
│       └── registry.rs
└── conqueror-core-tests/
    ├── Cargo.toml
    └── tests/
        └── messaging.rs
```

Keep `conqueror-macros` out of the first pass. Manual metadata is acceptable until the runtime shape is proven.

## Core API sketch

This is not final API design; it is the smallest shape worth testing.

```rust
pub trait Message: Send + Sync + 'static {
    type Response: Send + Sync + 'static;

    const TAG: &'static str;
}

#[derive(Clone, Debug, Default)]
pub struct Context {
    operation_id: OperationId,
    data: ContextData,
}

pub struct MessageRequest<M> {
    pub message: M,
    pub context: Context,
}

#[async_trait::async_trait]
pub trait MessageHandler<M>: Send + Sync + 'static
where
    M: Message,
{
    async fn handle(&self, request: MessageRequest<M>) -> Result<M::Response, ConquerorError>;
}
```

The Tower adapter should be internal:

```rust
pub(crate) struct HandlerService<H, M> {
    handler: H,
    marker: PhantomData<M>,
}
```

`HandlerService<H, M>` implements:

```rust
Service<MessageRequest<M>, Response = M::Response, Error = ConquerorError>
```

The registry erases typed services only after middleware has been applied.

## Caller API sketch

The caller experience should stay operation-centric:

```rust
let app = Conqueror::builder()
    .add_message::<CreateOrder, _>(CreateOrderHandler)
    .build();

let response = app
    .messages()
    .send(CreateOrder {
        product_id: "book".to_owned(),
        quantity: 1,
    })
    .await?;
```

A middleware-capable registration can be less elegant in the MVP, as long as the concept is proven:

```rust
let app = Conqueror::builder()
    .add_message_with::<CreateOrder, _, _>(CreateOrderHandler, |service| {
        ServiceBuilder::new()
            .layer(test_observation_layer())
            .layer(test_authorization_layer())
            .service(service)
    })
    .build();
```

If this shape becomes intolerable, that is useful evidence. Do not hide it behind macros in the MVP.

## Implementation sequence

### 1. Establish the Rust workspace

Create the experimental workspace and add only the dependencies needed for the message core:

- `tokio`
- `tower`
- `async-trait`
- `thiserror`
- `uuid` or a smaller operation ID dependency

Avoid Serde until a transport or metadata test needs it.

### 2. Write the first failing test

Test a single message registration and dispatch through the public API:

```rust
struct GetCounterValue {
    counter_name: String,
}

struct GetCounterValueResponse {
    value: i32,
}

impl Message for GetCounterValue {
    type Response = GetCounterValueResponse;
    const TAG: &'static str = "get-counter-value";
}
```

Acceptance criteria:

1. Register `GetCounterValueHandler`.
2. Send `GetCounterValue`.
3. Receive `GetCounterValueResponse`.
4. The test uses the public dispatcher/facade, not direct handler invocation.

### 3. Implement the minimum message core

Implement:

- `Message`
- `MessageRequest<M>`
- `MessageHandler<M>`
- `HandlerService<H, M>`
- `ConquerorBuilder`
- `MessageRegistry`
- `MessageDispatcher`
- `MessageSenders` or equivalent facade

The registry can use `TypeId` internally. Keep that type erasure private.

### 4. Add missing-handler behavior

Write a failing test for sending an unregistered message.

Acceptance criteria:

1. Dispatch returns a typed `ConquerorError::HandlerNotFound`.
2. The error includes at least the message type name or tag.
3. No panic, no silent fallback.

### 5. Add handler error propagation

Write a failing test for a handler returning an error.

Acceptance criteria:

1. The caller receives the same meaningful error category.
2. The dispatcher does not wrap everything into an opaque string.
3. The error model remains usable for future transport mapping.

### 6. Prove Tower-backed middleware

Write failing tests for middleware behavior before polishing the API.

Acceptance criteria:

1. Middleware can observe the request before the handler.
2. Middleware can observe the response after the handler.
3. Middleware ordering is deterministic.
4. Middleware can short-circuit without invoking the handler.
5. Middleware errors propagate to the caller.

Use simple test layers, not logging/auth production layers.

### 7. Add explicit context

Write failing tests for context creation and visibility.

Acceptance criteria:

1. A root context is created when the caller does not supply one.
2. A caller-supplied context reaches middleware and handler.
3. Each dispatched message gets an operation ID.
4. Context data can be read and written in-process.

Do not implement upstream/downstream transport flow yet. The MVP only needs the local context model that future transports can build on.

### 8. Add compile-time metadata only if needed

If manual `impl Message` noise distracts from the prototype, add a minimal derive macro in a second crate:

```rust
#[derive(Message)]
#[message(response = CreateOrderResponse, tag = "create-order")]
struct CreateOrder {
    product_id: String,
    quantity: i32,
}
```

Acceptance criteria:

1. The macro only generates `impl Message`.
2. It does not perform registration.
3. It does not generate HTTP, OpenAPI, schema, or transport metadata.

If the manual API is acceptable, skip this entirely.

## Validation suite

The MVP is complete only when tests prove:

1. typed request/response dispatch;
2. missing-handler error behavior;
3. handler error propagation;
4. middleware before/after ordering;
5. middleware short-circuiting;
6. context visibility in middleware and handler;
7. operation ID assignment;
8. no public API requires callers to touch `Any`, `TypeId`, boxed erased requests, or Tower internals for the basic path.

## Evaluation criteria

After the MVP, judge the spike brutally:

1. **Ergonomics:** Is the caller API close enough to Conqueror's operation-centric model?
2. **Local reasoning:** Can a user find the message, handler, registration, and call site without macro magic?
3. **Tower fit:** Did Tower integration simplify the design, or did it force awkward types everywhere?
4. **Type erasure:** Is the heterogeneous registry understandable and contained?
5. **Context model:** Does explicit context feel clean, or does it infect every user-facing signature?
6. **Testability:** Are black-box tests straightforward?
7. **Extensibility:** Is an Axum transport obviously possible without rewriting the core?

If two or more of these fail, do not proceed to HTTP. Fix the message core or stop.

## Follow-up phases after a successful MVP

Only after the message core passes the evaluation:

1. Add an Axum HTTP message transport.
2. Add transport conformance tests for in-process and HTTP message transport.
3. Add Serde-backed metadata and payload serialization.
4. Consider a tiny derive macro for message metadata.
5. Re-evaluate whether signals belong in the same runtime or should be a separate crate.
6. Design iterator protocol separately, likely around WebSockets or Tonic streaming.

## Risks

### Heterogeneous registry complexity

The core difficulty is storing many differently typed services behind one registry while preserving typed `send<M>()`. Keep all downcasting private and test it hard.

### Tower type explosion

Tower stacks can produce huge concrete types. The MVP should erase service types at registration boundaries after applying layers.

### Error model drift

Avoid stringly errors. The error type must remain suitable for future HTTP/gRPC mapping.

### Macro temptation

Macros can make ugly APIs look acceptable. Do not add macros until the manual model is proven.

### Context overreach

Do not reproduce .NET ambient context. Keep context explicit in `MessageRequest<M>` and defer transport flow semantics.

## Stop conditions

Stop the Rust effort if:

1. basic typed dispatch requires unacceptable boilerplate;
2. Tower integration leaks into every common user interaction;
3. the registry requires unsafe code;
4. context propagation becomes implicit global state;
5. tests need heavy mocking to validate the public API.

These failures would indicate that a Rust Conqueror would not preserve the qualities that make the .NET library worth using.
