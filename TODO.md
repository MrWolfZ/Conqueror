# Open points for Conqueror libraries

This file contains all the open points for extensions and improvements to the **Conqueror** libraries. It is a pragmatic solution before switching fully to GitHub issues for task management.

## General

- [ ] set up issues templates via yaml config
- [ ] add code coverage reports and badge

## Common

- [ ] in development environment, add problem details body to auth failures
- [ ] throw on empty context data key

## CQS

- [ ] add `.Has()` method to pipelines
- [ ] add pipeline builder methods to throw on duplicate middleware
- [ ] add trace logging to transports
- [ ] allow custom method name for custom handler interfaces
  - [ ] redirect `IncrementCounter` to `Handle` in interface
  - [ ] caller and handler use custom method name
  - [ ] dynamic proxy delegates the custom method back to handle on the proxy
  - [ ] handler registration asserts that the method has a compatible signature
- [ ] update all recipes and examples to drop `Command` and `Query` suffixes + also use more speaking response names, e.g. `IncrementCounter` and `CounterIncremented`
- [ ] align all tests names to `Given_When_Then` style
- [ ] add recipe for dynamic pipelines (based on e.g. transport type or `IConfiguration`)
- [ ] create benchmark app
  - [ ] add benchmarks for running with and without context items
- [ ] write code-level documentation for all public APIs
- [ ] add null checks to public API methods to support users that do not use nullable reference types
- [ ] cache client factory results
- [ ] allow multiple server-side transports (e.g. can offer query through HTTP and other transport at once) but on client enforce that a client uses a single transport
- [ ] add tests for handlers that throw exceptions to assert contexts are properly cleared
- [ ] create analyzers (including code fixes)
  - [ ] remove analyzers for missing or incorrect `ConfigurePipeline` methods
  - [ ] non-empty `ConfigurePipeline` method
  - [ ] custom handler interfaces may not have extra methods
  - [ ] handler must not implement multiple custom interfaces for same command
  - [ ] middlewares must not implement more than one middleware interface of the same type (i.e. not implement interface with and without configuration)
  - [ ] error (optionally) when a handler is being injected directly instead of an interface
- [ ] allow registering all custom interfaces in assembly as clients with `AddConquerorCommandClientsFromAssembly(Assembly assembly, Action configureTransport)`
- [ ] use a source generator instead of reflection for generating proxies
- [ ] add a quick reference handbook that showcases all capabilities in a concise fashion
- [ ] create dedicated readme files for each package
- [ ] use explicit dependency version numbers in all recipes and examples
  - [ ] add a script to bump version number across whole project

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

- [ ] configure publisher pipeline at the call site instead of during publisher registration (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/5042ec259b4a7f720dbbc09dd1732d8c41ddec90))
- [ ] refactor context data tests to use smarter test case generation (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/db5b68d1fbfd1e408e3ad3965dd013c5b3e0fd2a))
- [ ] create transport client before pipeline execution (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/91e86f4b287e22782645b01e48f1ac1bedb96bfe))
- [ ] make pipeline builder interface generic (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/725c336cd1648be0ac2212073e2fbcab73fa493f))
- [ ] remove "builder" from pipeline extension class names (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/ec2740d5bdab398a7bcd12fa9e6eda61985af13b))
- [ ] add generic parameters for pipeline references in xml docs (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/a19a2577a24f5340ef1fe3d1f30149cf37dcb1e7))
- [ ] ensure proper exception is thrown when invalid handler interface is used when registering client (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/23c2ae510d853c065e5c54c53e61c640679514ed))
- [ ] ensure that full stack trace is contained in exception logs (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/c4a9419b896fa225372ae348d76c31ef8715a78f))
- [ ] improve performance by using loop instead of recursion in pipeline (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/0e7fe9634ac7ca528d44d61276afba37009150a1))
- [ ] refactor pipeline logic to take middleware instances instead of resolving them (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/25adf2f62dd3abf55753bbb39261ddc0d924aeb1))
- [ ] expose transport type on middleware context (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/325c9365f155488d8ecc6727a467d54806706b68))
  - [ ] including http and client/server
  - [ ] add helper methods to check transport type
  - [ ] expose `UseInMemory` transport builder extension method
- [ ] make middleware interface generic in query and response type (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/7936901f02180babcbc8ad974552e4120a2842e7))
- [ ] adjust recipes to new middleware conventions (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/1b1534dd6238f615b4767da985c1120a30430574))
- [ ] rename inmemory to inprocess transport (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/da80ff49cf0a6653bf602c8e1216c1594f81dd45))
- [ ] expose transporttype on pipeline in addition to middleware context to enable conditional pipelines (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/abbb6066826c3d710bafd5db4ff32db3f17cf50c))
- [ ] rename `HandleEvent` to `Handle` (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/ed3f2e4cb86f9daf9784f4f40708d9d7031d6276))
- [ ] add support for delegate middlewares (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/f52d1341e59519e09cad38abc766bd79219344c5))
- [ ] make pipeline enumerable (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/f306f5a8c3030eb104d9915eed49f15190153a32))
- [ ] add transport type info to log output (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/652f8610456333a9e8eba40056738c0517d160b7))
- [ ] ensure that dynamically generated in-process clients create a correct transport with the handler type and pipeline (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/caa8a3a3e52618a83f5f30b4e48ad65bf087986b))
- [ ] properly propagate event IDs (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/ea08dc4420033656ef5012079bfa63830078bd4d))
- [ ] simplify functionality tests and ensure all combinations are covered (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/04cbe0d57f52b23abc91534ccbd2533576e2a19a))
- [ ] refactor all tests to use `Assert.That` for exceptions
- [ ] add dedicated solution
- [ ] add `.Has()` method to pipelines
- [ ] add trace logging to transports
- [ ] write tests to ensure that client pipeline sees transport type as observer and handler sees it as publisher
- [ ] when registering client, validate it is not a concrete class
- [ ] update all recipes and examples to drop `Event` suffix
- [ ] align all tests names to `Given_When_Then` style
- [ ] add recipe for dynamic pipelines (based on e.g. transport type or `IConfiguration`)
- [ ] create benchmark app (see [CQS](https://github.com/MrWolfZ/Conqueror/commit/65f4197bf2df717cc387f74bacd264ae519f9e53))
  - [ ] add benchmarks for running with and without context items
- [ ] provide HTTP websocket transport
  - [ ] add test for edge case where different observers for the same event type are configured with different remote hosts and ensure that events are only dispatched to correct observers
  - [ ] add test for edge case where transport initialization throws (expect that hosted service retries with exponential backoff)
- [ ] provide file system transport
  - [ ] use file system as persistent queue with file watcher / poller on observer side
- [ ] when configuring middlewares, apply configuration to all instances of the middleware in the pipeline
- [ ] add pipeline builder methods to throw on duplicate middleware
- [ ] write code-level documentation for all public APIs
- [ ] built-in `Synchronization` observer middleware
- [ ] add pipeline builder methods to throw on duplicate middleware
- [ ] handling and tests for conqueror context
- [ ] add null checks to public API methods to support users that do not use nullable reference types
- [ ] for event observers, create one background service per transport that inits all the observers for that transport
- [ ] for event observers, ignore missing transports even if event type is annotated with that transport
- [ ] add a quick reference handbook that showcases all capabilities in a concise fashion
- [ ] create dedicated readme files for each package
- [ ] use explicit dependency version numbers in all recipes and examples
  - [ ] add a script to bump version number across whole project

### Eventing middleware

- [ ] create projects for common middlewares, e.g.
  - [ ] `Conqueror.Eventing.Middleware.Logging`
  - [ ] `Conqueror.Eventing.Middleware.DataAnnotationValidation` (only for publisher)
  - [ ] `Conqueror.Eventing.Middleware.FluentValidation` (only for publisher)
  - [ ] `Conqueror.Eventing.Middleware.Metrics`
  - [ ] `Conqueror.Eventing.Middleware.Tracing`

## Streaming

- [ ] refactor context data tests to use smarter test case generation
- [ ] create transport client before pipeline execution
- [ ] expose transport type on middleware context
  - [ ] including http and client/server
  - [ ] add helper methods to check transport type
  - [ ] expose `UseInMemory` transport builder extension method
- [ ] integrate pipeline configuration interface into producer interface
- [ ] make pipeline builder interface generic
- [ ] move producer and consumer code into dedicated directories
- [ ] refactor pipeline logic to take middleware instances instead of resolving them
- [ ] add support for delegate middlewares
- [ ] add `.Has()` method to pipelines
- [ ] make pipeline enumerable
- [ ] improve performance by using loop instead of recursion in pipeline
- [ ] rename inmemory to inprocess transport
- [ ] remove lifetime support from handlers (explain in recipe that this was an explicit design decision and that lifetimes encourage bad practices by making handlers stateful when they should be stateless; also show how you can use injected classes to compensate, e.g. `IMemoryCache`)
- [ ] expose transporttype on pipeline in addition to middleware context to enable conditional pipelines
- [ ] add trace logging to transports
- [ ] write tests to ensure that client pipeline sees transport type as consumer and handler sees it as producer
- [ ] when registering client, validate it is not a concrete class
- [ ] rename `ExecuteRequest` and `HandleItem` to `Handle`
- [ ] update all recipes and examples to drop `Request` suffixes + also use more speaking names, e.g. `GetCounterIncrements` and `CounterIncremented`
- [ ] align all tests names to `Given_When_Then` style
- [ ] add recipe for dynamic pipelines (based on e.g. transport type or `IConfiguration`)
- [ ] create benchmark app
  - [ ] add benchmarks for running with and without context items
- [ ] add test to assert that disposing async enumerator calls finally blocks in handler/server
- [ ] add tests for middleware lifetimes when applied to different handler types
- [ ] add context support to consumer
- [ ] add null checks to public API methods to support users that do not use nullable reference types
- [ ] add transport for SignalR
- [ ] add recipes
  - [ ] create a recipe that shows how to use these handlers to read from an external stream (e.g. Kafka topic)
    - [ ] show how to handle acknowledgement by wrapping the item in an envelope
- [ ] add a quick reference handbook that showcases all capabilities in a concise fashion
- [ ] create dedicated readme files for each package
- [ ] use explicit dependency version numbers in all recipes and examples
  - [ ] add a script to bump version number across whole project

### Streaming middleware

- [ ] create projects for common middlewares, e.g.
  - [ ] `Conqueror.Streaming.Middleware.Logging`
  - [ ] `Conqueror.Streaming.Middleware.DataAnnotationValidation` (only for publisher)
  - [ ] `Conqueror.Streaming.Middleware.FluentValidation` (only for publisher)
  - [ ] `Conqueror.Streaming.Middleware.Metrics`
  - [ ] `Conqueror.Streaming.Middleware.Tracing`

### Streaming HTTP transport

- [ ] add tests for behavior when websocket connection is interrupted (i.e. disconnect without proper close handshake)
  - [ ] consider adding explicit message for signaling the end of the stream
- [ ] propagate conqueror context
- [ ] allow setting prefetch options (e.g. buffer size, prefetch batch size)
