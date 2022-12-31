# Open points for Conqueror libraries

This file contains all the open points for extensions and improvements to the **Conqueror** libraries. It is a pragmatic solution before switching fully to GitHub issues for task management.

## General

- [ ] clean up all code
- [ ] link nuget packages for all supporting packages in the libaries section of each core lib in readme (e.g. transports and middlewares)
- [ ] use `SuppressMessage` instead of pragmas everywhere for suppressing diagnostics
- [ ] use file-scoped namespaces everywhere
- [ ] re-order all code so that command comes before query (or even better split files)
- [ ] add documentation about being able to use pipelines internally for external API calls
- [ ] for some features provide code snippets in documentation instead of library (e.g. common middlewares etc.)
- [ ] add code coverage reports and badge
- [ ] add null checks to public API methods to support users that do not use nullable reference types

## CQS

- [ ] carry a trace ID across commands and queries
- [ ] make types in `Conqueror.CQS.Common` public to allow third-party packages to use them (e.g. for custom transports)
- [ ] create analyzers (including code fixes)
  - [ ] when generating pipeline configuration method via code fix, also add comment for suppressing unused method (with extra comment about removing this comment when .NET 7 or 8 is being used)
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

### CQS middleware

- [ ] create projects for common middlewares, e.g.
  - [ ] `Conqueror.CQS.Middleware.Logging`
  - [ ] `Conqueror.CQS.Middleware.Tracing`
  - [ ] `Conqueror.CQS.Middleware.Timeout`
  - [ ] `Conqueror.CQS.Middleware.Retry`

### CQS ASP Core

- [ ] carry a trace ID across commands and queries (read and write trace ID from/to standard HTTP tracing header)
- [ ] add `string? VersionString` parameter to command and query attribute
- [ ] add `uint? DefaultCommandVersion` etc. to client and server options
- [ ] add recipe for customizing OpenAPI specification with swashbuckle
- [ ] add client option for setting custom HTTP headers (e.g. for authentication)
- [ ] instruct users to place their custom path conventions into their contracts module to allow both server and client to use the same conventions
- [ ] allow registering commands and queries via DI extension instead of attribute

## Eventing

- [ ] use `.ConfigureAwait(false)` everywhere
- [ ] fix: do not register generic types during assembly scanning
- [ ] fix: do not register nested private types during assembly scanning
- [ ] make event publisher middleware pipeline configurable
- [ ] make event publishing strategy customizable
  - [ ] ship two strategies out of the box (parallel and sequential)
    - [ ] make them available as service collection extension methods
  - [ ] sequential strategy as default
  - [ ] handle cancellation in strategy
- [ ] add tests for service collection configuration
- [ ] for .NET 6 add analyzer that ensures the `ConfigurePipeline` method is present on all handlers with pipeline configuration interface (including code fix)

## Interactive streaming

- [ ] use `.ConfigureAwait(false)` everywhere
- [ ] fix: do not register generic types during assembly scanning
- [ ] fix: do not register nested private types during assembly scanning
- [ ] implement middleware support
- [ ] implement clients and transport infrastructure
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
