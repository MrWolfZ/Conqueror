# Open points for Conqueror libraries

This file contains all the open points for extensions and improvements to the **Conqueror** libraries. It is a pragmatic solution before switching fully to GitHub issues for task management.

## General

- [ ] set up issues templates via yaml config
- [ ] for each library add a quick reference handbook that showcases all capabilities in a concise fashion
- [ ] create dedicated readme files for each package
- [ ] use explicit dependency version numbers in all recipes and examples
  - [ ] add a script to bump version number across whole project
- [ ] add code coverage reports and badge
- [ ] add null checks to public API methods to support users that do not use nullable reference types

## Common

- [ ] instead of just marking execution from a transport in the conqueror context, set a field that contains the transport name
  - [ ] add this name to the logging output of logging middlewares
- [ ] in development environment, add problem details body to auth failures
- [ ] throw on empty context data key
- [ ] do not use base64 encoding for context data, but instead use cookie-like encoding (URL-encode the value)

## CQS

- [ ] add tests for overwriting handler registration with a new factory and assert that new factory is called
- [ ] add tests for overwriting handler registration with a new lifetime and assert that new lifetime is used
- [ ] add tests for overwriting middleware registration with a new factory and assert that new factory is called
- [ ] add tests for overwriting middleware registration with a new lifetime and assert that new lifetime is used
- [ ] add tests for registries for multiple delegate handlers to confirm that each handler is discoverable
- [ ] add tests that registering all types from assembly does not overwrite existing registrations
- [ ] adjust lifetime tests to assert on multiple executions of the same handler "instance"
- [ ] add tests that assert that service provider on pipeline builders is from correct scope
- [ ] add CQS to known abbreviations in rider settings and remove inconsistent naming suppressions
- [ ] when configuring middlewares, apply configuration to all instances of the middleware in the pipeline
- [ ] add pipeline builder methods to throw on duplicate middleware
- [ ] write code-level documentation for all public APIs
- [ ] cache client factory results
- [ ] add tests for handlers that throw exceptions to assert contexts are properly cleared
- [ ] create analyzers (including code fixes)
  - [ ] remove analyzers for missing or incorrect `ConfigurePipeline` methods
  - [ ] enforce `IConfigureCommandPipeline` interface to be present on all handler types that implement `ConfigurePipeline` method
  - [ ] non-empty `ConfigurePipeline` method
  - [ ] custom handler interfaces may not have extra methods
  - [ ] handler must not implement multiple custom interfaces for same command
  - [ ] enforce `IConfigureCommandPipeline` interface to only be present on command handler types
  - [ ] middlewares must not implement more than one middleware interface of the same type (i.e. not implement interface with and without configuration)
  - [ ] error (optionally) when a handler is being injected directly instead of an interface
- [ ] allow registering all custom interfaces in assembly as clients with `AddConquerorCommandClientsFromAssembly(Assembly assembly, Action<ICommandPipelineBuilder> configurePipeline)`
- [ ] re-order all code so that command comes before query (or even better split files)
- [ ] use a source generator instead of reflection for generating proxies

### CQS middleware

- [ ] in logging middleware use logging source generator with dynamic log level as seen [here]( https://andrewlock.net/exploring-dotnet-6-part-8-improving-logging-performance-with-source-generators/)
- [ ] create projects for common middlewares, e.g.
  - [ ] `Conqueror.CQS.Middleware.FluentValidation`
  - [ ] `Conqueror.CQS.Middleware.MemoryCache`
  - [ ] `Conqueror.CQS.Middleware.Metrics`
  - [ ] `Conqueror.CQS.Middleware.Tracing`
  - [ ] `Conqueror.CQS.Middleware.Synchronization`

### CQS ASP Core

- [ ] add test to ensure that adding server services multiple times adds options only once
  - [ ] allow configuration callback to run multiple times
- [ ] add recipe for customizing OpenAPI specification with Swashbuckle
- [ ] create analyzers (including code fixes)
  - [ ] when command or query does not have a version
- [ ] provide delegating HTTP client handler that takes care of conqueror context propagation to allow custom http clients to be used

## Eventing

- [ ] refactor all tests to use `Assert.That` for exceptions
- [ ] provide HTTP websocket transport
  - [ ] add test for edge case where different observers for the same event type are configured with different remote hosts and ensure that events are only dispatched to correct observers
  - [ ] add test for edge case where transport initialization throws (expect that hosted service retries with exponential backoff)
- [ ] when configuring middlewares, apply configuration to all instances of the middleware in the pipeline
- [ ] add pipeline builder methods to throw on duplicate middleware
- [ ] write code-level documentation for all public APIs
- [ ] built-in `Synchronization` observer middleware
- [ ] add pipeline builder methods to throw on duplicate middleware
- [ ] handling and tests for conqueror context
- [ ] for .NET 6 add analyzer that ensures the `ConfigurePipeline` method is present on all handlers with pipeline configuration interface (including code fix)

### Eventing middleware

- [ ] create projects for common middlewares, e.g.
  - [ ] `Conqueror.Eventing.Middleware.Logging`
  - [ ] `Conqueror.Eventing.Middleware.DataAnnotationValidation` (only for publisher)
  - [ ] `Conqueror.Eventing.Middleware.FluentValidation` (only for publisher)
  - [ ] `Conqueror.Eventing.Middleware.Metrics`
  - [ ] `Conqueror.Eventing.Middleware.Tracing`

## Interactive streaming

- [ ] add test to assert that disposing async enumerator calls finally blocks in handler/server
- [ ] fix: do not register generic types during assembly scanning
- [ ] fix: do not register nested private types during assembly scanning
- [ ] implement middleware support
- [ ] implement clients and transport infrastructure
- [ ] expose service provider on middleware context
- [ ] handling and tests for conqueror context
- [ ] add streaming request context
- [ ] add handler registry
- [ ] add transport for SignalR
- [ ] for .NET 6 add analyzer that ensures the `ConfigurePipeline` method is present on all handlers with pipeline configuration interface (including code fix)

design ideas:

- [ ] request handlers and item handlers are registered separately
  - [ ] both have independent pipelines
  - [ ] a request handler can be used without an item handler
  - [ ] a handler can be used without a request handler (by simply invoking the `HandleItem` method)
    - [ ] this executes the pipeline
- [ ] request handlers have the transport logic, item handlers are always in-process
- [ ] `ItemHandlerRunner` service, which takes a request and item type, resolves the request and item handlers are runs it to completion
  - [ ] e.g. `Task RunStream<TRequest, TItem>(CancellationToken cancellationToken)`
- [ ] extra hosting package, which has a hosted service for running item handlers
  - [ ] handlers opt into this with a custom service collection extension, e.g. `AddConquerorInteractiveStreamingBackgroundService(o => o.AddItemHandler<THandler>())`
- [ ] create a recipe that shows how to use these handlers to read from an external stream (e.g. Kafka topic)
  - [ ] show how to handle acknowledgement by wrapping the item in an envelope

### Interactive streaming ASP Core

- [ ] refactor implementation to use transport client
- [ ] add path convention mechanism
- [ ] ensure api description works
- [ ] add tests for behavior when websocket connection is interrupted (i.e. disconnect without proper close handshake)
  - [ ] consider adding explicit message for signaling the end of the stream
- [ ] propagate conqueror context
- [ ] delegate route path creation to service
- [ ] allow setting prefetch options (e.g. buffer size, prefetch batch size)

## Reactive streaming

- [ ] implement basic version
- [ ] implement middleware support
