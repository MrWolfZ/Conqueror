# Conqueror

A set of libraries to powercharge your .NET development.

## Open points

- edge cases (i.e. write tests)
  - core
    - registering handler as singleton instance
- pass through custom interface extra methods to backing instance
- reduce use of open generics in DI
- remove middleware config interfaces
- allow middlewares to implement multiple interfaces
- ensure middlewares can call next middleware multiple times
- specify middleware order through pipeline builder
  - make config attribute on context nullable
- allow skipping middleware through option on base attribute
- allow creating aggregation middleware attributes that apply a specific set of middlewares at once
  - allow overriding options of aggregation attribute with explicit attribute
- during configuration phase validate that all referenced middlewares are present, and that query handlers only have query middlewares and command handlers only have command middlewares
- add support for .NET standard 2.0
- use `.ConfigureAwait(false)` everywhere
- add null checks to public API methods
- add trace IDs to commands and queries
  - integrate trace ID from ASP Core if using HTTP package
- allow accessing command and query context via accessor interface (async local)
- change HTTP client options to expose provider through options object instead of using factory properties
- allow registering all custom interfaces in assembly as HTTP client (allow specifying options for all those clients)
- pull some logic for HTTP into a `Http.Common` package
  - for example default strategy for routes
- http client edge cases (i.e. write tests)
  - complex query objects for GET
  - custom serializer settings for read/write
  - null properties
  - null GET parameters
  - throw error on double http service registration
- event handling

  - make event publishing strategy customizable
  - ship two strategies out of the box (parallel and sequential)
    - make them available as service collection extension methods
  - sequential strategy as default
  - handle cancellation in strategy
  - edge cases (i.e. write tests)
    - core
      - registering same observer multiple times

- for some features provide code snippets in documentation instead of library (e.g. common middlewares etc.)
- add analyzer package that ensures a set of given middlewares is applied to all command and query handlers
- create `Conqueror` and `Conqueror.Abstractions` entry packages that combine CQS and Eventing
