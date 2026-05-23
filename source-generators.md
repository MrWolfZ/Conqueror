# Source generator findings
## Scope and files read
- `/home/dev/src/conqueror/src/core/Conqueror.SourceGenerators/Messaging/MessageTypeGenerator.cs:9-183`
- `/home/dev/src/conqueror/src/core/Conqueror.SourceGenerators/Messaging/MessageHandlerTypeGenerator.cs:7-67`
- `/home/dev/src/conqueror/src/core/Conqueror.SourceGenerators/Signalling/SignalTypeGenerator.cs:7-100`
- `/home/dev/src/conqueror/src/core/Conqueror.SourceGenerators/Iterating/IteratorTypeGenerator.cs:9-171`
- `*Sources.cs` emitters for message/signal/iterator and handler types
- `/home/dev/src/conqueror/src/core/Conqueror.SourceGenerators.Tests/TestHelpers.cs:19-337`
- `/home/dev/src/conqueror/src/core/Conqueror.SourceGenerators.Tests/Messaging/MessagingGeneratorTests.cs:11-103`
- `/home/dev/src/conqueror/src/core/Conqueror.SourceGenerators.Tests/Signalling/SignallingGeneratorTests.cs:11-100`
- `/home/dev/src/conqueror/src/core/Conqueror.SourceGenerators.Tests/Iterating/IteratingGeneratorTests.cs`
- snapshot expectations under `.../TestCases/**/snapshot.verified.txt`
- runtime contracts: `/home/dev/src/conqueror/src/core/Conqueror.Abstractions/{Messaging,Signalling,Iterating}/*.cs`

## Generated artifacts and responsibilities
- **Message types**: `partial record/class <T> : IMessage<T, TResponse>` with `T`, `CoreTypesInjector`, nested `IHandler`, `IPipeline`, proxies, `InvokeHandler`, `EmptyInstance`, `JsonSerializerContext`, `PublicConstructors`, `PublicProperties`, plus transport-specific interfaces/properties.
- **Signal types**: same pattern for `ISignal<TSignal>`.
- **Iterator types**: same pattern for `IIterator<TIterator,TItem>` and `IAsyncEnumerable<TItem>`.
- **Handler types**: `partial class/record Handler : *HandlerWithSourceGeneration`, transport-specific handler interfaces, `GetTypeInjectors()`, and `[ModuleInitializer] RegisterHandlerType<...>()`.
- Golden outputs exist in AOT fixtures (`Conqueror.Tests.AOT/Generated/...`) and snapshot tests, confirming exact emitted shape.

## How generated code connects to runtime
- Abstractions demand generator-owned members to avoid reflection/AOT: `CoreTypesInjector`, `EmptyInstance`, `JsonSerializerContext`, `PublicConstructors`, `PublicProperties`, `InvokeHandler`.
- `IMessageHandler` / `ISignalHandler` / `IIteratorHandler` expose `GetTypeInjectors()` and pipeline hooks; generated code fills these in for each concrete handler.
- Generated nested `IHandler` types tie the message/signal/iterator type to its handler type, enabling fluent proxy APIs and compile-time type inference.
- Module initializers are the bootstrap path for runtime registries (`MessageHandlerTypeServiceRegistry`, `SignalHandlerTypeServiceRegistry`, `IteratorHandlerTypeServiceRegistry`).

## Design patterns extracted
- Static-abstract-members + nested partial types = compile-time type synthesis.
- Explicit metadata (`JsonSerializerContext`, reflection lists, empty instance) = AOT-friendly, no runtime scanning.
- Self-typed handler/proxy pattern = fluent `WithPipeline` / `WithTransport` APIs.
- Core + transport injectors = additive multi-transport registration.
- User-defined `GetTypeInjectors` / `ModuleInitializer` short-circuit generation when already present.

## Rust macro equivalent
- Use proc macros on message/signal/iterator items, generating associated traits/impls and helper modules.
- Prefer `derive`/attribute macros with associated types over nested interfaces.
- Use explicit registry crates (`inventory`, `linkme`) or explicit registration functions for handler bootstrapping.
- Map `JsonSerializerContext` to `serde` plus generated serializers / schema metadata.

## Portability risks and recommendations
- Proc macros are weaker than Roslyn for cross-crate inspection and “does user already implement this?” checks.
- Rust async handlers need boxed futures or `async-trait`, with ergonomics/perf tradeoffs.
- Trait-object/object-safety constraints make the C# generic proxy style hard to mirror 1:1.
- Dynamic registration is less natural than module initializers; explicit registration is safer.
- Feasible in Rust, but best as a simplified macro/registry design rather than a direct transliteration.
