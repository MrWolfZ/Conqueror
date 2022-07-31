# Conqueror

A set of libraries to powercharge your .NET development.

## Open points

### Breaking changes

- change root namespace to `Conqueror`
- make event publisher middleware pipeline configurable
- change HTTP client options to expose provider through options object instead of using factory properties

### Enhancements

- create common abstractions package
- expose contexts directly on middleware contexts
- allow middlewares to implement multiple interfaces
- validate pipeline configuration method signature
- pass through custom interface extra methods to backing instance
- add support for .NET standard 2.0
- add trace IDs to commands and query contexts
  - integrate trace ID from ASP Core if using HTTP package
- allow registering all custom interfaces in assembly as HTTP client (allow specifying options for all those clients)
- pull some logic for HTTP into a `Http.Common` package
  - for example default strategy for routes
- add analyzer that ensures the `ConfigurePipeline` method is present on all handlers (including code fix)

- eventing
  - make event publishing strategy customizable
  - ship two strategies out of the box (parallel and sequential)
    - make them available as service collection extension methods
  - sequential strategy as default
  - handle cancellation in strategy
- http client edge cases (i.e. write tests)
  - complex query objects for GET
  - custom serializer settings for read/write
  - null properties
  - null GET parameters
  - throw error on double http service registration
- http server edge cases
  - throw error when duplicate command name is found

- for some features provide code snippets in documentation instead of library (e.g. common middlewares etc.)
- create `Conqueror` and `Conqueror.Abstractions` entry packages that combine CQS and Eventing

### Bugfixes

- use `.ConfigureAwait(false)` everywhere
- add null checks to public API methods
