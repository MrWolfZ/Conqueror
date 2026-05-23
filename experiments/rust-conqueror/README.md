# Rust Conqueror experiment

This directory contains a draft Rust prototype for the smallest useful Conqueror message core.

It is a feasibility spike, not a production implementation and not a direct port of the .NET API.

## Goal

Prove whether Rust can support Conqueror's operation-centric request/response model with:

1. strongly typed messages and responses;
2. explicit handler registration;
3. explicit request context;
4. Tower-backed middleware;
5. private type erasure inside the registry;
6. black-box tests through the public dispatch facade.

The prototype intentionally excludes HTTP, gRPC, signals, iterators, file-system transport,
automatic registration, OpenAPI, and macros.

## Current status

The code has been written by static inspection only. The environment used to create it did not have
`cargo` or `rustc`, so it has not been compiled.

The draft already includes:

- `conqueror-core`: the message API, context model, Tower handler adapter, erased registry, and dispatcher facade.
- `conqueror-core-tests`: black-box tests for the MVP behavior.

## Structure

```txt
experiments/rust-conqueror/
├── Cargo.toml
├── conqueror-core/
│   ├── Cargo.toml
│   └── src/
│       ├── context.rs
│       ├── dispatch.rs
│       ├── error.rs
│       ├── handler.rs
│       ├── lib.rs
│       ├── message.rs
│       └── registry.rs
└── conqueror-core-tests/
    ├── Cargo.toml
    ├── src/lib.rs
    └── tests/messaging.rs
```

## Intended usage shape

```rust
let app = Conqueror::builder()
    .add_message::<CreateOrder, _>(CreateOrderHandler)
    .build();

let response = app.messages().send(CreateOrder {
    product_id: "book".to_owned(),
    quantity: 1,
}).await?;
```

Middleware is applied before the typed service is erased:

```rust
let app = Conqueror::builder()
    .add_message_with::<CreateOrder, _, _, _>(CreateOrderHandler, |service| {
        ServiceBuilder::new()
            .layer(test_observation_layer())
            .service(service)
    })
    .build();
```

## Validation when Rust is available

From this directory:

```bash
cargo test
cargo fmt --check
cargo clippy --all-targets -- -D warnings
```

Expected initial issues, if any, should be treated as design feedback rather than papered over. In
particular, do not hide API awkwardness behind macros until the manual model is proven.

## MVP behavior covered by tests

`conqueror-core-tests/tests/messaging.rs` covers:

1. dispatching a registered message to its handler;
2. returning a typed missing-handler error;
3. propagating handler errors;
4. middleware observing request/response flow;
5. middleware short-circuiting the handler;
6. middleware error propagation;
7. caller-supplied context reaching the handler;
8. unique operation IDs for root dispatch contexts.

## Design notes

- `MessageHandler<M>` is the user-facing trait.
- `HandlerService<H, M>` adapts handlers into Tower `Service<MessageRequest<M>>`.
- The registry stores erased `BoxCloneService` entries keyed by `TypeId`.
- Type erasure and downcasting are private to the registry/dispatcher boundary.
- Handlers are stored behind `Arc`, so handler types do not need to implement `Clone`.
- `Context::new()` creates a fresh operation ID; there is intentionally no `Default` implementation for `Context`.

## Stop conditions

Stop this experiment instead of expanding it if:

1. the core code cannot be made to compile without unsafe code;
2. Tower types leak into the common caller API;
3. typed dispatch requires unacceptable boilerplate;
4. context handling becomes implicit global state;
5. tests need mocks instead of public black-box dispatch.
