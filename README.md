# Conqueror

A set of libraries to powercharge your .NET development.

## Open points

- allow registering all handlers / observers in assembly
- for some features provide code snippets in documentation instead of library (e.g. common middlewares etc.)
- add option to specify order for middleware in attribute
  - default order is registration order in DI container
  - instead of attribute you can also provide an order during DI registration
- add analyzer package that ensures a set of given middlewares is applied to all command and query handlers
- add trace IDs to commands and queries
  - integrate trace ID from ASP Core if using HTTP package
- pull some logic for HTTP into a `Http.Common` package
  - for example default strategy for routes
- throw error on double http service registration
- align tests in CQS lib with Eventing lib
- event handling

  - make event publishing strategy customizable
  - ship two strategies out of the box (parallel and sequential)
    - make them available as service collection extension methods
  - sequential strategy as default
  - handle cancellation in strategy

- edge cases (i.e. write tests)
  - core
    - registering same handler multiple times
    - registering different handlers with same signature
    - resolving singleton handler via interface, self, and IXHandler should return same instance
    - custom handler interface with extra methods
  - http client
    - complex query objects for GET
    - custom serializer settings for read/write
    - null properties
    - null GET parameters

