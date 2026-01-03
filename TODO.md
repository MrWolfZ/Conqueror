# Open points for Conqueror libraries

This file contains all the open points for extensions and improvements to the **Conqueror** libraries. It is a pragmatic solution before switching fully to GitHub issues for task management.

## General

- [ ] write script to generate solution and `.dotsettings` for recipes
- [ ] set up issues templates via yaml config
- [ ] add code coverage reports and badge
- [ ] run separate matrix steps for different dotnet versions and ensure that all code works if running against only dotnet 8

## Core

### Common

- [ ] throw on empty context data key
- [ ] change dependencies to depend on greater-than 8
- [ ] create benchmark app
  - [ ] add benchmarks for running with and without context items
- [ ] use explicit dependency version numbers in all recipes and examples
  - [ ] add a script to bump version number across whole project
- [ ] consider making the `ConquerorContext` opt-out-able or even opt-in (would require passing certain things like IDs in a different way)
- [ ] move "internal" interfaces (which need to be public in the abstractions because generated code relies on them) to a separate internal namespace

### Messaging

- [ ] align receiver configs to not return itself, but either void or a configuration object
- [ ] in types injectors rename the `Create` method to `Inject`
- [ ] add tests for cancellation that assert a `Handle` throws early if passed a canceled token, and also cancels after every middleware
- [ ] use `ValueTask` instead of `Task` in internal APIs
  - [ ] validate using benchmarks that this improves latency and memory usage
- [ ] add test that pipeline can be safely forked
- [ ] add sender to context data test location
- [ ] add test to assert that messages support polymorphism
- [ ] add `.Has()` method to pipelines
- [ ] add pipeline builder methods to throw on duplicate middleware
- [ ] align all tests names to `Given_When_Then` style
- [ ] write code-level documentation for all public APIs
- [ ] add null checks to public API methods to support users that do not use nullable reference types
- [ ] add tests for handlers that throw exceptions to assert contexts are properly cleared
- [ ] add a quick reference handbook that showcases all capabilities in a concise fashion
- [ ] allow opting into performance enhancement by re-using pipelines
  - [ ] add `WithEagerPipeline` to evaluate client pipeline eagerly, which allows caching senders with a pre-built pipeline
  - [ ] add `ConfigureStaticPipeline` to create a handler pipeline only once

### Signalling

- [ ] in types injectors rename the `Create` method to `Inject`
- [ ] add tests for cancellation that assert a `Handle` throws early if passed a canceled token, and also cancels after every middleware
- [ ] refactor `UseInProcess()` to return builder for setting broadcast strategy instead of overloads
- [ ] add parameter for sequential strategy that configures whether to abort early on cancellation
- [ ] implement fire & forget strategy
- [ ] add aggregate publisher that takes a broadcast strategy
- [ ] use `ValueTask` instead of `Task` in internal APIs
  - [ ] validate using benchmarks that this improves latency and memory usage
- [ ] add test that pipeline can be safely forked
- [ ] add publisher to context data test location
- [ ] add `.Has()` method to pipelines
- [ ] add pipeline builder methods to throw on duplicate middleware
- [ ] align all tests names to `Given_When_Then` style
- [ ] write code-level documentation for all public APIs
- [ ] add null checks to public API methods to support users that do not use nullable reference types
- [ ] add tests for handlers that throw exceptions to assert contexts are properly cleared
- [ ] add recipe that showcases how batching can be implemented with a custom transport
- [ ] add docs that specify that the sequential strategy calls observers in an unspecified order
- [ ] add a quick reference handbook that showcases all capabilities in a concise fashion
- [ ] allow opting into performance enhancement by re-using pipelines
  - [ ] add `WithEagerPipeline` to evaluate client pipeline eagerly, which allows caching senders with a pre-built pipeline
  - [ ] add `ConfigureStaticPipeline` to create a handler pipeline only once

### Iterators

- [ ] implement
- [ ] add recipes
  - [ ] create a recipe that shows how to use these handlers to read from an external stream (e.g. Kafka topic)
    - [ ] show how to handle acknowledgement by wrapping the item in an envelope
- [ ] add a quick reference handbook that showcases all capabilities in a concise fashion

### Source Generator

- [ ] test whether source gen can be included transitively through abstractions during development instead of having to explicitly reference it everywhere
- [ ] generate `string? SummaryComment` property
- [ ] write tests for all situations and diagnostics (using data driven tests)
  - [ ] for every single property, assert that the property can be manually defined and the generator will skip it
  - [ ] test that the generator works even if the marker interfaces are explicitly implemented, including when through a base class
- [ ] generate property metadata (name, type, is-required, is-nullable, etc.)
  - [ ] ensure to also add `string? SummaryComment`
- [ ] generate `CreateInstance(IReadOnlyDictionary<string, object>)` method
  - [ ] skip if user-defined function exists to allow for special cases
  - [ ] support primary constructors, required properties, and any combination
  - [ ] recursively instantiate property objects, again enforcing single constructor
  - [ ] emit error diagnostic if more than one constructor is defined
- [ ] add statement in recipe that attributes can be renamed with a global using if they cause conflicts
- [ ] improve error messages

### Analyzers

- [ ] create analyzers (including code fixes)
  - [ ] enforce correct `ConfigurePipeline` method signature
    - [ ] do this generically (for any handler type) by finding signature mismatches between any `static virtual` interface method and methods of the same name on the handler type
  - [ ] enforce non-empty `ConfigurePipeline` method

## Middlewares

- [ ] in logging middleware recipe explain how to use payload logging kind
- [ ] reimplement data annotation middleware
  - [ ] mark it as requiring reflection
- [ ] create projects for common middlewares, e.g.
  - [ ] `Conqueror.Middleware.FluentValidation`
  - [ ] `Conqueror.Middleware.MemoryCache`
  - [ ] `Conqueror.Middleware.Metrics`
  - [ ] `Conqueror.Middleware.Semaphore`
  - [ ] `Conqueror.Middleware.RateLimiting`
  - [ ] `Conqueror.Middleware.Tracing`

### Logging

- [ ] allow configuring properties which are added to the logging scope
- [ ] allow supplying custom logger factory
- [ ] make stack trace capture opt-in rather than opt-out

### Authorization

- [ ] add an explanation why no implementation for signals is provided

## Transport General

- [ ] create transport test utils package that contains a list of baseline tests that all transports must fulfill
  - [ ] functionality
  - [ ] context data
  - [ ] trace ID
  - [ ] will require converting base class approach to test case generation approach so that only a single test class needs to be subclassed

## Transport.Http

- [ ] assert that well-known error handling middleware only sets response if it has not already started yet and otherwise rethrows
- [ ] add API docs tests for [Microsoft OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-9.0) and [Scalar](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-9.0#use-scalar-for-interactive-api-documentation)
- [ ] add chaos tests

### Transport.Http Messaging

- [ ] consider 3XX response status codes as error
- [ ] support templated paths
  - [ ] provide custom query or body serializer in IHttpMessage based on HTTP method (instead of lazily deciding this on server/client)
  - [ ] in body serializer set content type with utf-8 encoding and in deserializer throw if content type is not json or encoding is explicitly set to something non-utf8
  - [ ] in both add support for binding from path template
- [ ] add tests for running with compression middleware
  - [ ] see also [this article](https://stackoverflow.com/questions/28754673/httpclient-conditionally-set-acceptencoding-compression-at-runtime) and [this one](https://www.tpeczek.com/2017/08/aspnet-core-response-compression.html) for some inspiration
- [ ] add tests for running with handler with disabled redirect following (should fail message)
- [ ] allow adding endpoints explicitly or manually and then `MapMessageEndpoints` skips those message types
- [ ] add tests that a custom base interface can be used to specify custom conventions
- [ ] add `FailureStatusCodes` property to `IHttpMessage` (defaults to 400, 401, and 403)
  - [ ] generate appropriate endpoint metadata
  - [ ] ensure that swashbuckle can be used to customize it, e.g. adding `ProblemDetails` as body schema
- [ ] add support for URI templates (see [this implementation](https://github.com/modelcontextprotocol/csharp-sdk/blob/adb2098e4d847ae6075f98a06b8fbca8c057f6cb/src/ModelContextProtocol/UriTemplate.cs#L19) for reference)
  - [ ] the serializer interface gets the path parameters in a dictionary (see [this answer](https://stackoverflow.com/questions/56461701/how-to-read-uri-parameters-using-httpcontextaccessor-in-asp-net-core/56462148#56462148) for reference)
  - [ ] for default body serialization, the URI parameters are only used for routing, the payload is still fully read from the content
- [ ] add trace logging (only if ILoggerFactory is present)
- [ ] add test to assert that messages support polymorphism
- [ ] create `HttpMessageEndpointDescriptor` and pass that around internally when registering endpoints instead of accessing `TMessage` everywhere
- [ ] add summary comment to API descriptions
- [ ] in development environment, when message fails, add exception message and stack trace to response body
- [ ] add recipe for customizing OpenAPI specification with Swashbuckle
- [ ] create analyzers (including code fixes)
  - [ ] when message does not have a version

### Transport.Http Signalling

- [ ] consider 3XX response status codes as unrecoverable error
- [ ] add API descriptions for endpoints
- [ ] add tests for running with compression middleware
- [ ] add tests for running with handler with disabled redirect following (should be unrecoverable receiver connection error)
- [ ] SSE improvements
  - [ ] add option to send server heartbeats for idle connections
    - [ ] configurable interval, either regular interval or debounced
  - [ ] in `MapSignalSseEndpoints` add overload which takes an `IHttpSseEndpointConfiguration`, which allows authorizing requests based on requested signal event types, and allows configuring a pipeline (with a `ConfigurePipeline<TSignal>(ISignalPipeline<TSignal> pipeline) where TSignal : IHttpSseSignal` method)
    - [ ] add option to enforce `Accept` header
  - [ ] [for reference](https://github.com/tpeczek/Lib.AspNetCore.ServerSentEvents/blob/main/Lib.AspNetCore.ServerSentEvents/ServerSentEventsMiddleware.cs)
- [ ] in web sockets recipes, explain that the web sockets middleware needs to be added
- [ ] in web sockets recipes, explain how to handle CORS (see [for reference](https://www.siakabaro.com/raw-websocket-in-csharp-aspnet-core/))

### Transport.Http Iterators

- [ ] provide SSE transport
  - [ ] make it very explicit in docs that `next` calls are not propagated to the server which means there is no backpressure, which can cause issues when the server publishes faster than the client consumes (i.e. the client buffer fills up)
- [ ] provide HTTP websocket transport
  - [ ] add tests for behavior when websocket connection is interrupted (i.e. disconnect without proper close handshake)
    - [ ] consider adding explicit message for signaling the end of the stream
  - [ ] allow setting prefetch options (e.g. buffer size, prefetch batch size)

## Transport.SignalR

### Transport.SignalR Messaging

- [ ] provide SignalR transport

### Transport.SignalR Signalling

- [ ] provide SignalR transport

### Transport.SignalR Iterators

- [ ] provide SignalR transport

## Transport.Database

- [ ] write a generic database transport which defines the abstractions (`DatabaseSignalAttribute`, `UseDatabase`, `RunDatabaseSignalReceiver`, etc.) and provides interfaces like `ISignalDatabaseWriter` and `ISignalDatabaseReader` to be implemented by specific database packages

### Transport.Database Messaging

- [ ] provide database transport

### Transport.Database Signalling

- [ ] provide database transport

### Transport.Database Iterators

- [ ] provide database transport

## Transport.FileSystem

### Transport.FileSystem Messaging

- [ ] provide file system transport
  - [ ] use file system as persistent queue with file watcher / poller on sender / receiver sides

### Transport.FileSystem Signalling

- [ ] provide file system transport
  - [ ] use file system as persistent buffer with file watcher / poller on receiver side that indexes into the buffer

### Transport.FileSystem Iterators

- [ ] provide file system transport
  - [ ] use file system as persistent queue and buffer with file watcher / poller on runner / receiver sides with sender indexing into the buffer

## Transport.Redis

- [ ] use [Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/testing/write-your-first-test?pivots=nunit) or [test containers](https://omerugi.medium.com/boost-your-integration-tests-sharing-a-redis-container-with-testcontainers-for-net-8fe8c01d98ec) to spawn a container during testing
  - [ ] use [NUnit SetUpFixture](https://docs.nunit.org/articles/nunit/writing-tests/attributes/setupfixture.html) to start one container for all tests

### Transport.Redis Messaging

- [ ] provide redis transport

### Transport.Redis Signalling

- [ ] provide redis transport

### Transport.Redis Iterators

- [ ] provide redis transport

## Examples

- [ ] enhance examples with auth by first redirecting to user selection page
  - [ ] on selection get bearer token from server (with symmetric in-memory signing key)
    - [ ] do not use cookies to allow different tabs with different users
  - [ ] send messages with bearer token header
  - [ ] allow logging out to change user
- [ ] add example for SSE signal endpoint
  - [ ] also handle authentication by lazily running receiver and configuring it with the bearer token from a singleton
- [ ] add example for SSE iterator endpoint
- [ ] add AOT example app
- [ ] create multiple implementations of basic scenario
  - [ ] chat app
  - [ ] messages to send chats and DMs
  - [ ] signals to receive public chats
  - [ ] iterators to receive DMs
