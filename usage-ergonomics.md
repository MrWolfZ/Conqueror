# Usage ergonomics findings
## Scope and files read
- `/home/dev/src/conqueror/src/core/recipes/messaging/getting-started/README.md:49-429`
- `/home/dev/src/conqueror/src/core/recipes/messaging/testing-handlers/README.md:9-260`
- `/home/dev/src/conqueror/src/core/recipes/messaging/getting-started/.completed/Conqueror.Recipes.Messaging.GettingStarted/Program.cs:1-75`
- `/home/dev/src/conqueror/src/core/Conqueror.Tests/Messaging/MessageHandlerRegistrationTests.cs:7-219`
- `/home/dev/src/conqueror/src/core/Conqueror.Tests/Messaging/MessageMiddlewareConfigurationTests.cs:6-201`
- `/home/dev/src/conqueror/src/core/Conqueror.Tests/Messaging/MessageTypeGenerationTests.cs:21-195`
- `/home/dev/src/conqueror/recipes/cqs/advanced/exposing-via-http/README.md:29-190`
- `/home/dev/src/conqueror/recipes/cqs/advanced/testing-calling-http/README.md:52-219`

## User-facing workflows
- Message definition is attribute + partial record + nested interface handler: `[Message<GetCounterValueResponse>] public partial record GetCounterValue(string CounterName);` and `GetCounterValueHandler : GetCounterValue.IHandler` (`getting-started/README.md:186-204`, `242-258`, `296-326`, `373-387`).
- Fire-and-forget is the same shape with `[Message]` and a `Task Handle(...)` returning no response (`getting-started/README.md:373-387`).
- At call sites users resolve `IMessageSenders` and invoke `senders.For(GetCounterValue.T).Handle(new(counterName))` (`Program.cs:16-63`, `README.md:219-227`, `276-280`, `345-349`, `393-397`).
- Automatic registration is a one-liner via `services.AddMessageHandlersFromAssembly(typeof(Program).Assembly)` (`getting-started/README.md:330-343`, `421-429`).
- HTTP exposure is declarative: `[HttpCommand]`, `[HttpQuery]`, plus `AddConquerorCQSHttpControllers()` and `app.UseConqueror()` (`exposing-via-http/README.md:29-76`).

## API ergonomics patterns
- Strong “message as API” model: public surface is the message type, not the handler class (`testing-handlers/README.md:9-31`, `27-31`).
- Generic nested handler interface gives compile-time pairing and keeps handler names close to messages (`MessageTypeGenerationTests.cs:53-88`, `111-170`).
- Assembly scanning reduces registration noise but still allows explicit `AddMessageHandler<T>()` and variants; tests show type/factory/instance/delegate registration all supported (`MessageHandlerRegistrationTests.cs:49-129`).
- Pipeline configuration is first-class and fluent (`pipeline.Use(...)`, `pipeline.UseWhen(...)`, `pipeline.Configure<Middleware>(...)`) (`MessageMiddlewareConfigurationTests.cs:13-21`, `40-49`, `117-130`).
- HTTP customization is attribute-driven but still allows escape hatches with custom controllers and path conventions (`exposing-via-http/README.md:87-190`).

## Testing and recipes
- Recommended testing style is black-box via messages, not direct handler invocation (`testing-handlers/README.md:27-31`).
- Minimal test setup: build `ServiceCollection`, register handler(s), resolve `IMessageSenders`, then invoke `For(...).Handle(...)` (`testing-handlers/README.md:63-105`).
- More ergonomic recipe extracts a base class with shared DI setup and `MessageSenders` helper (`testing-handlers/README.md:169-200`, `210-259`).
- For HTTP client tests, they replace transport clients with delegate handlers or a test server (`testing-calling-http/README.md:52-80`, `114-189`).

## What must be preserved in Rust
- Discoverability: a message type should still be the thing users search for, not a hidden registration key.
- Low-boilerplate invocation at call sites (`send/handle/publish/iterate` style) with transport/pipeline hidden behind a façade.
- Automatic registration by module/assembly-equivalent discovery, plus explicit opt-in overrides.
- Separate concerns for transport, pipeline, and handler logic.

## Rust API sketch implications
- Derive macro on message structs/enums (`#[derive(Message)]`) can generate the nested/associated handler trait, metadata, and a type token analogous to `T`.
- Handlers can be `async_trait` impls of generated traits; zero-response messages can map to `()` or a dedicated unit type.
- Registration can use builder methods on `ServiceCollection`/DI equivalents, with module scanning or explicit `.add_handler::<H>()`.
- Pipelines map cleanly to Tower layers/middleware; per-message config could be done via builder closures.
- Transport exposure could be done with Axum/Actix extractors or route generation, but Rust likely needs more explicit types for HTTP request/response mapping.

## Risks and trade-offs
- Rust will likely lose some of the “just write a record and nested handler” magic because macros can’t fully emulate C# source-generator-generated nested interfaces without more ceremony.
- Compile-time ergonomics may improve for trait bounds and ownership, but dynamic assembly scanning is weaker in Rust; module registration is usually more explicit.
- Middleware/pipeline composition could be better in Rust via Tower, but async-trait and generic type complexity may make the user-facing API feel more verbose.
- Conqueror’s strongest simplification is hiding DI + transport + pipeline behind one message-centric abstraction; Rust can preserve the model, but probably not the same level of implicit discovery/registration without macro-heavy ergonomics.
