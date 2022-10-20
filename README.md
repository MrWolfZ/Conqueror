# Conqueror

A set of libraries to powercharge your .NET development.

## Open points

- for some features provide code snippets in documentation instead of library (e.g. common middlewares etc.)
- use `.ConfigureAwait(false)` everywhere
- add null checks to public API methods to support users that do not use nullable reference types

### CQS

- expose command/query and conqueror context objects directly on middleware contexts
- pass through custom interface extra methods to backing instance
- for .NET 6 add analyzer that ensures the `ConfigurePipeline` method is present on all handlers with pipeline configuration interface (including code fix)

#### CQS ASP Core

- delegate route path creation to service
- allow attaching middlewares to http clients (via the configuration action)
- allow registering all custom interfaces in assembly as HTTP clients with `AddConquerorHttpClientsFromAssembly(Assembly assembly, Func<IServiceProvider, HttpClient> httpClientFactory)`
- allow path to be set for http commands and queries

- add missing tests
  - complex query objects for GET
  - custom serializer settings for read/write
  - null properties
  - null GET parameters
  - throw error on double http service registration

- http server edge cases
  - throw error when duplicate command name is found

### Eventing

- make event publisher middleware pipeline configurable
- make event publishing strategy customizable
  - ship two strategies out of the box (parallel and sequential)
    - make them available as service collection extension methods
  - sequential strategy as default
  - handle cancellation in strategy
- add tests for service collection configuration
- for .NET 6 add analyzer that ensures the `ConfigurePipeline` method is present on all handlers with pipeline configuration interface (including code fix)

### Interactive streaming

- implement middleware support
- for .NET 6 add analyzer that ensures the `ConfigurePipeline` method is present on all handlers with pipeline configuration interface (including code fix)

#### Interactive streaming ASP Core

- add tests for behavior when websocket connection is interrupted (i.e. disconnect without proper close handshake)
  - consider adding explicit message for signaling the end of the stream
- propagate conqueror context
- delegate route path creation to service
- allow setting prefetch options (e.g. buffer size, prefetch batch size)

### Reactive streaming

- implement basic version
- implement middleware support
