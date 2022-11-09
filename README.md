# Conqueror

A set of libraries to powercharge your .NET development.

## Open points

- for some features provide code snippets in documentation instead of library (e.g. common middlewares etc.)
- use `.ConfigureAwait(false)` everywhere
- add null checks to public API methods to support users that do not use nullable reference types

### CQS

- create analyzers (including code fixes)
  - non-empty `ConfigurePipeline` method
  - enforce `IConfigureCommandPipeline` interface to be present on all handler types that implement `ConfigurePipeline` method
  - enforce `IConfigureCommandPipeline` interface to be present on all command handler types
  - enforce `IConfigureCommandPipeline` interface to only be present on command handler types
  - custom handler interfaces may not have extra methods
  - handler must not implement multiple custom interface for same command
  - middlewares must not implement more than one middleware interface of the same type (i.e. not implement interface with and without configuration)
- rename `CommandHandlerMetadata` to `CommandHandlerRegistration` and make it public in the abstractions to allow external libraries to use it for server transports
  - same for queries
- add tests for handlers that throw exceptions to assert contexts are properly cleared
- expose command/query and conqueror context objects directly on middleware contexts
- allow registering all custom interfaces in assembly as clients with `AddConquerorCommandClientsFromAssembly(Assembly assembly, Action<ICommandPipelineBuilder> configurePipeline)`
- when .NET 7 is released skip analyzers that are superfluous with .NET 7 (i.e. everything related to pipeline configuration methods)
  - adjust tests to expect compilation error diagnostic instead of analyzer diagnostic

    ```cs
    // in analyzer
    if (context.SemanticModel.SyntaxTree.Options is CSharpParseOptions opt && opt.PreprocessorSymbolNames.Contains("NET7_0_OR_GREATER"))
    {
        return;
    }

    // in verifiers
    project = project.WithParseOptions(((CSharpParseOptions)project.ParseOptions!).WithLanguageVersion(LanguageVersion.Latest)
                                                                                  .WithPreprocessorSymbols("NET7_0_OR_GREATER"));
    ```

#### External feedback

- confusing that AddConquerorCQS is duplicate name for adding normal services and HTTP services
- use source generators to generate partial class to allow custom code while using common infra
- rename ConfigureConqueror() to make it more clear what it does and that it must be done as the final step (e.g. FinalizeConquerorRegistration)
- adjust super package description to make it clear that it does not contain any transports
- add documentation about being able to use pipelines internally for external API calls
- when generating pipeline configuration method via code fix, also add comment for suppressing unused method (with extra comment about removing this comment when .NET 7 or 8 is being used)
- consider making cancellation token parameter optional (with `= default`)
- add analyzer that errors (optionally) when a handler is being injected directly instead of an interface
- add analyzer that errors when `AddConquerorCQS` is being called without registration finalization method being called

#### CQS middleware

- create projects for common middlewares, e.g.
  - `Conqueror.CQS.Middleware.Timeout`
  - `Conqueror.CQS.Middleware.Retry`

#### CQS ASP Core

- rename libraries to `Conqueror.CQS.Transport.Http.Client` and `Conqueror.CQS.Transport.Http.Server.AspNetCore`
- delegate route path creation to service
  - in config for client middleware allow setting path convention
    - instruct users to place their route convention into their contracts module to allow both server and client to use the same convention
  - allow path to be set for http commands and queries via attribute
  - allow version to be set for http commands and queries via attribute
- allow complex objects in GET queries by JSON-serializing them
- allow registering commands and queries via DI extension instead of attribute

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
- implement clients and transport infrastructure
- for .NET 6 add analyzer that ensures the `ConfigurePipeline` method is present on all handlers with pipeline configuration interface (including code fix)

#### Interactive streaming ASP Core

- refactor implementation to use transport client
- ensure api description works
- add tests for behavior when websocket connection is interrupted (i.e. disconnect without proper close handshake)
  - consider adding explicit message for signaling the end of the stream
- propagate conqueror context
- delegate route path creation to service
- allow setting prefetch options (e.g. buffer size, prefetch batch size)

### Reactive streaming

- implement basic version
- implement middleware support
