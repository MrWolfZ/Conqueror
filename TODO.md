# Open points for Conqueror libraries

This file contains all the open points for extensions and improvements to the **Conqueror** libraries. It is a pragmatic solution before switching fully to GitHub issues for task management.

## General

- [ ] set up issues templates via yaml config
- [ ] in all recipe code that is at risk of being copy-pasted, add a note that it shouldn't be used and add a reference to pre-built packages or recipes where possible (e.g. for middlewares)
- [ ] in recipes improve language, e.g. replace `Now that ...` with `As the next step ...` etc.
- [ ] for each library add a quick reference handbook that showcases all capabilities in a concise fashion
- [ ] use file-scoped namespaces everywhere
- [ ] add nunit analyzers to all test projects and refactor all tests to use modern test assertions
- [ ] configure build pipelines to build project with a variety of SDK versions
  - [ ] set up dedicated example projects for .NET 6 and 7 with older SDK versions to ensure analyzers can be referenced without warnings or errors
- [ ] re-order all code so that command comes before query (or even better split files)
- [ ] add code coverage reports and badge
- [ ] add null checks to public API methods to support users that do not use nullable reference types
- [ ] use explicit dependency version numbers in all recipes and examples
  - [ ] add a script to bump version number across whole project

## CQS

- [ ] add note in basics recipe that it is fine for simple applications to have DB logic directly in handlers
- [ ] remove restriction about only being able to add middleware to pipeline once (also adjust mentions in recipe)
- [ ] try referencing a lower version of csharp analyzer dependencies to prevent version conflicts
- [ ] write code-level documentation for all public APIs
- [ ] create analyzers (including code fixes)
  - [ ] do not raise analyzer error for missing `ConfigurePipeline` on .NET 7 or higher
  - [ ] when generating pipeline configuration method via code fix, also add comment for suppressing unused method (with extra comment about removing this comment when .NET 7 or higher is being used)
  - [ ] non-empty `ConfigurePipeline` method
  - [ ] enforce `IConfigureCommandPipeline` interface to be present on all handler types that implement `ConfigurePipeline` method
  - [ ] enforce `IConfigureCommandPipeline` interface to only be present on command handler types
  - [ ] custom handler interfaces may not have extra methods
  - [ ] handler must not implement multiple custom interface for same command
  - [ ] middlewares must not implement more than one middleware interface of the same type (i.e. not implement interface with and without configuration)
  - [ ] error (optionally) when a handler is being injected directly instead of an interface
  - [ ] error when `AddConquerorCQS` is being called without registration finalization method being called
- [ ] add tests for handlers that throw exceptions to assert contexts are properly cleared
- [ ] allow registering all custom interfaces in assembly as clients with `AddConquerorCommandClientsFromAssembly(Assembly assembly, Action<ICommandPipelineBuilder> configurePipeline)`
- [ ] allow caching client factory results

### CQS middleware

- [ ] create projects for common middlewares, e.g.
  - [ ] `Conqueror.CQS.Middleware.DataAnnotationValidation`
  - [ ] `Conqueror.CQS.Middleware.Retry`
  - [ ] `Conqueror.CQS.Middleware.Timeout`
  - [ ] `Conqueror.CQS.Middleware.Metrics`
  - [ ] `Conqueror.CQS.Middleware.Tracing`
  - [ ] `Conqueror.CQS.Middleware.FluentValidation`

### CQS ASP Core

- [ ] create recipe for "testing code that calls HTTP commands or queries"
  - [ ] in recipe, mention that optimally you would test against the real application
  - [ ] in recipe, show how to create dedicated test server that has a delegate for handling specific command (the delegate calls another delegate, that can be overwritten in a test to be able to execute assertions)
- [ ] create analyzers (including code fixes)
  - [ ] when command or query does not have a version
- [ ] add recipe for customizing OpenAPI specification with swashbuckle
- [ ] instruct users to place their custom path conventions into their contracts module to allow both server and client to use the same conventions
- [ ] provide delegating HTTP client handler that takes care of conqueror context propagation to llow custom http clients to be used
- [ ] allow registering commands and queries via DI extension instead of attribute

## Eventing

- [ ] refactor to use transport concept
  - [ ] publisher uses attribute on event to determine on which transport to publish
  - [ ] observer requires explicit registration with transport client
  - [ ] provide HTTP websocket transport
- [ ] fix: do not register generic types during assembly scanning
- [ ] fix: do not register nested private types during assembly scanning
- [ ] handling and tests for conqueror context
- [ ] add event context
- [ ] add handler registry
- [ ] expose service provider on middleware context
- [ ] make event publisher middleware pipeline configurable
- [ ] make event publishing strategy customizable
  - [ ] ship two strategies out of the box (parallel and sequential)
    - [ ] make them available as service collection extension methods
  - [ ] sequential strategy as default
  - [ ] handle cancellation in strategy
- [ ] add tests for service collection configuration
- [ ] for .NET 6 add analyzer that ensures the `ConfigurePipeline` method is present on all handlers with pipeline configuration interface (including code fix)

## Interactive streaming

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
