<!-- markdownlint-disable MD033 -->

# Conqueror - a highly ergonomic library for building structured, scalable .NET apps

**Conqueror** is a .NET library that simplifies writing modular, scalable applications by unifying messages, signals, and async iterators into a consistent, extensible transport-agnostic model. It uses modern features of .NET like source generators and static abstract interface methods to reduce boilerplate, support advanced uses cases like AOT compilation, and to provide a highly ergonomic user-friendly API.

## Why does Conqueror exist?

Author's note: In my opinion, all of the many existing .NET messaging libraries lack in one or more of the areas developer experience, testability, and documentation. With **Conqueror** I set out to design an API that closes that gap.

## Core Design Principles of Conqueror

- provide an excellent developer experience with discoverable APIs, proper IDE integration (e.g. ensuring that "Go to ..." shortcuts work as expected), and great use-case-driven documentation
- ensure that it is simple to test any code written using **Conqueror** and that any documentation for writing production code also explains how to test that code
- avoid hidden control flow, global state, and global behaviors wherever possible; instead, ensure that all API usage occurs in user code where it makes it obvious (and debuggable) for the developer what is happening
- everything on top of the core in-process functionality is optional, and user-written extensions can be just as powerful as pre-built extension packages
- encourage good architecture practices, but provide the freedom to build everything from monoliths to microservices without forcing developers into any particular direction

<img src="./docs/intro.svg?raw=true" alt="Intro" style="height: 565px" height="565px" />

[![Build Status](https://github.com/MrWolfZ/Conqueror/actions/workflows/dotnet.yml/badge.svg)](https://github.com/MrWolfZ/Conqueror/actions/workflows/dotnet.yml)
[![NuGet version (Conqueror)](https://img.shields.io/nuget/v/Conqueror?label=Conqueror)](https://www.nuget.org/packages/Conqueror/)
[![.NET 8 or later](https://img.shields.io/badge/.NET-8_or_later-blue)](https://dotnet.microsoft.com/en-us/download)
[![license](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

> **Conqueror** only supports [.NET 8 or later](https://dotnet.microsoft.com/en-us/download)

<!-- TOC -->
* [Quickstart](#quickstart)
* [Recipes](#recipes)
  * [Messaging Introduction](#messaging-introduction)
    * [Messaging Basics](#messaging-basics)
    * [Messaging Advanced](#messaging-advanced)
    * [Messaging Expert](#messaging-expert)
    * [Messaging Cross-Cutting Concerns](#messaging-cross-cutting-concerns)
  * [Signalling Introduction](#signalling-introduction)
    * [Signalling Basics](#signalling-basics)
    * [Signalling Advanced](#signalling-advanced)
    * [Signalling Expert](#signalling-expert)
    * [Signalling Cross-Cutting Concerns](#signalling-cross-cutting-concerns)
  * [Iterating Introduction](#iterating-introduction)
    * [Iterating Basics](#iterating-basics)
    * [Iterating Advanced](#iterating-advanced)
    * [Iterating Expert](#iterating-expert)
    * [Iterating Cross-Cutting Concerns](#iterating-cross-cutting-concerns)
* [Motivation](#motivation)
  * [Comparison with similar projects](#comparison-with-similar-projects)
    * [Differences to MediatR](#differences-to-mediatr)
    * [Differences to MassTransit](#differences-to-masstransit)
<!-- TOC -->

## Quickstart

This quickstart guide will let you jump right into the code without lengthy explanations. If you prefer more guidance, head over to our [recipes](#recipes). By following this quickstart guide, you'll add HTTP messages and an in-process signal to a minimal API ASP.NET Core application. You can also find the [source code](examples/quickstart) here in the repository.

```sh
dotnet new webapi -n Quickstart && cd Quickstart
dotnet add package Conqueror --prerelease
dotnet add package Conqueror.Middleware.Logging --prerelease
dotnet add package Conqueror.Transport.Http.Server.AspNetCore --prerelease
dotnet add package Swashbuckle.AspNetCore # to get a nice Swagger UI
```

Let's start by defining the contracts of our quickstart application in [Contracts.cs](examples/quickstart/Contracts.cs):

<!-- REPLACECODE examples/quickstart/Contracts.cs -->
```cs
namespace Quickstart;

using System.ComponentModel.DataAnnotations;
using Conqueror;

// In Conqueror, everything revolves around contracts of different kinds: messages, signals, and
// iterators (the latter is still experimental and therefore not yet included in the Quickstart).
// The contracts are simple records or classes marked by one or more attributes which determine the
// kind and transports (in-process, HTTP, gRPC, RabbitMQ, etc.) of the contract. A source generator
// is used to enhance the contracts with additional code, and therefore they must be partial

// Note that using transports is fully optional, and if you want you can use Conqueror purely
// in-process, similar to libraries like MediatR

// The `HttpMessage` attribute tells Conqueror that this message type can be exposed via HTTP
// (using the corresponding transport package). The attribute allows customizing the HTTP endpoint
// method, path, path prefix, version, API group name, etc. (note that all these are optional with
// sensible defaults, in this case leading to `POST /api/v1/incrementCounterByAmount`)
[HttpMessage<CounterIncrementedResponse>(Version = "v1")]
public sealed partial record IncrementCounterByAmount(string CounterName)
{
    // We use simple data annotation validation as an example, but more powerful validation
    // tools like FluentValidation are also supported. Note that the built-in .NET data annotation
    // validation is only supported for properties, not constructor parameters
    [Range(minimum: 1, long.MaxValue)]
    public required long IncrementBy { get; init; }
}

public sealed record CounterIncrementedResponse(long NewCounterValue);

// By default, HTTP messages are sent and received as POST, but all methods are supported.
// Parameters can be optional, and messages can have enumerable responses as well
[HttpMessage<List<CounterValue>>(HttpMethod = "GET", Version = "v1")]
public sealed partial record GetCounters(string? Prefix = null);

public sealed record CounterValue(string CounterName, long Value);

// Signals are a pub/sub mechanism, and can be handled in-process (like we do in this quickstart)
// or published via a transport like RabbitMQ (using the corresponding transport package)
[HttpSseSignal]
public sealed partial record CounterIncremented(
    string CounterName,
    long NewValue,
    long IncrementBy);
```

> <details>
> <summary>Click here to see file without the comments to get a better idea how your own code will look like</summary>
>
> [Contracts.cs](examples/quickstart.enhanced/Contracts.cs)
>
> <!-- REPLACECODE examples/quickstart.enhanced/Contracts.cs -->
> ```cs
> namespace Quickstart.Enhanced;
> 
> using System.ComponentModel.DataAnnotations;
> using Conqueror;
> 
> [HttpMessage<CounterIncrementedResponse>(Version = "v1")]
> public sealed partial record IncrementCounterByAmount(string CounterName)
> {
>     [Range(minimum: 1, long.MaxValue)]
>     public required long IncrementBy { get; init; }
> }
> 
> public sealed record CounterIncrementedResponse(long NewCounterValue);
> 
> [HttpMessage<List<CounterValue>>(HttpMethod = "GET", Version = "v1")]
> public sealed partial record GetCounters(string? Prefix = null);
> 
> public sealed record CounterValue(string CounterName, long Value);
> 
> [HttpSseSignal]
> public sealed partial record CounterIncremented(
>     string CounterName,
>     long NewValue,
>     long IncrementBy);
> ```
>
> </details>

In [CountersRepository.cs](examples/quickstart/CountersRepository.cs) create a simple repository to simulate talking to a database:

<!-- REPLACECODE examples/quickstart/CountersRepository.cs -->
```cs
namespace Quickstart;

using System.Collections.Concurrent;

// simulate a database repository (which is usually async)
internal sealed class CountersRepository
{
    private readonly ConcurrentDictionary<string, long> counters = [];

    public async Task<long> AddOrIncrementCounter(string counterName, long incrementBy)
    {
        await Task.Yield();

        return counters.AddOrUpdate(counterName, incrementBy, (_, value) => value + incrementBy);
    }

    public async Task<long> GetCounterValue(string counterName)
    {
        await Task.Yield();

        return counters.GetValueOrDefault(counterName, defaultValue: 0L);
    }

    public async Task<IReadOnlyDictionary<string, long>> GetCounters()
    {
        await Task.Yield();

        return counters;
    }
}
```

In [IncrementCounterByAmountHandler.cs](examples/quickstart/IncrementCounterByAmountHandler.cs) create a message handler for our `IncrementCounterByAmount` message type.

<!-- REPLACECODE examples/quickstart/IncrementCounterByAmountHandler.cs -->
```cs
namespace Quickstart;

using System.ComponentModel.DataAnnotations;
using Conqueror;

// The handler type is also enhanced by the Conqueror source generator, so it must be partial
internal sealed partial class IncrementCounterByAmountHandler(
    CountersRepository repository,
    ISignalPublishers publishers
)
    // This interface (among other things) is generated by a source generator
    : IncrementCounterByAmount.IHandler
{
    // Configure a pipeline of middlewares which is executed for every message
    public static void ConfigurePipeline(IncrementCounterByAmount.IPipeline pipeline) =>
        pipeline
            // Conqueror ships with a handful of useful middleware packages
            // for common cross-cutting concerns like logging and authorization
            .UseLogging()
            // Pipelines can have inline middlewares for ad-hoc logic (or you can
            // build a full-fledged middleware; see the recipes for more details)
            .Use(ctx =>
            {
                // Perform a simple data annotation validation (in a real application you would
                // likely use a more powerful validation library like FluentValidation)
                Validator.ValidateObject(
                    ctx.Message,
                    new(ctx.Message),
                    validateAllProperties: true
                );

                // The middleware has access to the message with its proper type (i.e. the
                // compiler knows that `ctx.Message` is of type `IncrementCounterByAmount`),
                // so you could also write the validation directly like this:
                if (ctx.Message.IncrementBy <= 0)
                {
                    throw new ValidationException(
                        $"increment amount must be positive, but was {ctx.Message.IncrementBy}"
                    );
                }

                return ctx.Next(ctx.Message, ctx.CancellationToken);
            })
            // The `Use...` methods add middlewares to the pipeline, and afterward they can be
            // configured further, which is useful for extracting common configuration into a
            // shared method and then configure it per handler, e.g. like this:
            // `pipeline.UseDefault().WithIndentedJsonPayloadLogFormatting()`
            .ConfigureLogging(c =>
                c.MessagePayloadLoggingStrategy = PayloadLoggingStrategy.IndentedJson
            );

    public async Task<CounterIncrementedResponse> Handle(
        IncrementCounterByAmount message,
        CancellationToken cancellationToken = default
    )
    {
        var newValue = await repository.AddOrIncrementCounter(
            message.CounterName,
            message.IncrementBy
        );

        // `ISignalPublishers` is a factory to get a publisher for a signal type. The
        // `CounterIncremented.T` property is generated by the source generator and is used for
        // type inference. The 'For' method returns a `CounterIncremented.IHandler` (which is a
        // proxy used to publish the signal)
        await publishers
            .For(CounterIncremented.T)
            // When publishing a signal (or sending a message, etc.), you can also
            // specify a pipeline, which is executed before the transport is called.
            // And when the transport delivers the payload, the handler's own pipeline
            // is executed as well
            .WithPipeline(p => p.UseLogging())
            // The 'Handle' method is unique for each `IHandler`. This means that your
            // IDE's "Go to Implementation" feature will show all signal handlers for
            // this signal, making it simple to find all the places in your code where
            // a signal is used
            .Handle(new(message.CounterName, newValue, message.IncrementBy), cancellationToken);

        // You can also customize the transport which is used to publish the signal, for example,
        // to publish it via HTTP server-sent events. Note that it is also possible to do publish
        // to multiple transports at the same time using `b.UseAggregate()`; the details for this
        // can be found in the recipes
        await publishers
            .For(CounterIncremented.T)
            .WithPipeline(p => p.UseLogging())
            .WithTransport(b => b.UseHttpServerSentEvents())
            .Handle(new(message.CounterName, newValue, message.IncrementBy), cancellationToken);

        return new CounterIncrementedResponse(
            await repository.GetCounterValue(message.CounterName)
        );
    }
}
```

> <details>
> <summary>Click here to see a more realistic trimmed down version of the file</summary>
>
> [IncrementCounterByAmountHandler.cs](examples/quickstart.enhanced/IncrementCounterByAmountHandler.cs)
>
> <!-- REPLACECODE examples/quickstart.enhanced/IncrementCounterByAmountHandler.cs -->
> ```cs
> namespace Quickstart.Enhanced;
> 
> using Conqueror;
> 
> internal sealed partial class IncrementCounterByAmountHandler(
>     CountersRepository repository,
>     ISignalPublishers publishers
> ) : IncrementCounterByAmount.IHandler
> {
>     public static void ConfigurePipeline(IncrementCounterByAmount.IPipeline pipeline) =>
>         pipeline.UseDefault();
> 
>     public async Task<CounterIncrementedResponse> Handle(
>         IncrementCounterByAmount message,
>         CancellationToken cancellationToken = default
>     )
>     {
>         var newValue = await repository.AddOrIncrementCounter(
>             message.CounterName,
>             message.IncrementBy
>         );
> 
>         await publishers
>             .For(CounterIncremented.T)
>             .WithDefaultPublisherPipeline(typeof(IncrementCounterByAmountHandler))
>             .WithInProcessAndServerSentEventsTransport()
>             .Handle(new(message.CounterName, newValue, message.IncrementBy), cancellationToken);
> 
>         return new CounterIncrementedResponse(
>             await repository.GetCounterValue(message.CounterName)
>         );
>     }
> }
> ```
>
> </details>

In [DoublingCounterIncrementedHandler.cs](examples/quickstart/DoublingCounterIncrementedHandler.cs) create a signal handler that doubles increment operations on specific counters.

<!-- REPLACECODE examples/quickstart/DoublingCounterIncrementedHandler.cs -->
```cs
namespace Quickstart;

using Conqueror;

internal sealed partial class DoublingCounterIncrementedHandler(IMessageSenders senders)
    : CounterIncremented.IHandler
{
    // Signal handlers support handling multiple signal types (by adding more `IHandler`
    // interfaces), so the pipeline configuration is generic and is reused for all signal types
    // (`typeof(T)` can be checked to customize the pipeline for a specific signal type)
    static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline) =>
        pipeline
            .Use(ctx =>
            {
                // we are only interested in specific signals, so we skip the handler (and the
                // rest of the pipeline) for all others
                if (ctx.Signal is CounterIncremented { CounterName: "doubler" })
                {
                    return ctx.Next(ctx.Signal, ctx.CancellationToken);
                }

                return Task.CompletedTask;
            })
            .Use(ctx =>
            {
                // Below in the 'Handle' method we call 'IncrementCounterByAmount' again,
                // which could lead to an infinite loop. Conqueror "flows" context data
                // across different executions, which is useful here to handle a signal
                // only once per HTTP request
                if (ctx.ConquerorContext.InProcessData.Get<bool>("doubled"))
                {
                    return Task.CompletedTask;
                }

                ctx.ConquerorContext.InProcessData.Set("doubled", value: true);

                return ctx.Next(ctx.Signal, ctx.CancellationToken);
            })
            // Middlewares in the pipeline are executed in the order that they are added.
            // We add the logging middleware to the pipeline only after the prior two
            // middlewares to ensure that only signals which are not skipped get logged
            .UseLogging(o => o.PayloadLoggingStrategy = PayloadLoggingStrategy.IndentedJson);

    public async Task Handle(
        CounterIncremented signal,
        CancellationToken cancellationToken = default
    )
    {
        await senders
            .For(IncrementCounterByAmount.T)
            // Message senders can also have pipelines and use different transports. The exact
            // same middlewares like logging, validation, error handling, etc. can be used on
            // both senders and handlers
            .WithPipeline(p => p.UseLogging())
            .WithTransport(b => b.UseInProcess())
            // The 'Handle' method is unique for each `IHandler`, so "Go to Implementation" in
            // your IDE will jump directly to your handler, enabling smooth code base navigation,
            // even across different projects and transports
            .Handle(
                new(signal.CounterName) { IncrementBy = signal.IncrementBy },
                cancellationToken
            );
    }

    // Handlers for signal types which have a transport type that requires configuration must
    // declare a method for configuring the receiver. However, since this handler is only used to
    // handle in-process signals, we simply disable the HTTP SSE receiver. If you were to use this
    // handler in a client application, you would need to enable the receiver with the correct
    // configuration (see the recipes for more details)
    static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver) =>
        receiver.Disable();
}
```

> <details>
> <summary>Click here to see a more realistic trimmed down version of the file</summary>
>
> [DoublingCounterIncrementedHandler.cs](examples/quickstart.enhanced/DoublingCounterIncrementedHandler.cs)
>
> <!-- REPLACECODE examples/quickstart.enhanced/DoublingCounterIncrementedHandler.cs -->
> ```cs
> namespace Quickstart.Enhanced;
> 
> using Conqueror;
> 
> internal sealed partial class DoublingCounterIncrementedHandler(IMessageSenders senders)
>     : CounterIncremented.IHandler
> {
>     static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline) =>
>         pipeline
>             .SkipSignalMatching((CounterIncremented s) => s.CounterName != "doubler")
>             .EnsureSingleExecutionPerOperation(nameof(DoublingCounterIncrementedHandler))
>             .UseDefault();
> 
>     public async Task Handle(
>         CounterIncremented signal,
>         CancellationToken cancellationToken = default
>     )
>     {
>         await senders
>             .For(IncrementCounterByAmount.T)
>             .WithDefaultSenderPipeline(typeof(DoublingCounterIncrementedHandler))
>             .Handle(
>                 new(signal.CounterName) { IncrementBy = signal.IncrementBy },
>                 cancellationToken
>             );
>     }
> 
>     static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver) =>
>         receiver.Disable();
> }
> ```
>
> </details>

In [GetCountersHandler.cs](examples/quickstart/GetCountersHandler.cs) create a message handler that returns a filtered list of counters.

<!-- REPLACECODE examples/quickstart/GetCountersHandler.cs -->
```cs
namespace Quickstart;

using Conqueror;

internal sealed partial class GetCountersHandler(CountersRepository repository)
    : GetCounters.IHandler
{
    public static void ConfigurePipeline(GetCounters.IPipeline pipeline) =>
        pipeline.UseLogging(c =>
        {
            // The pipeline has access to the service provider from the scope of the call to the
            // handler in case you need it to resolve some services
            var isDevelopment = pipeline
                .ServiceProvider.GetRequiredService<IHostEnvironment>()
                .IsDevelopment();

            // The logging middleware supports detailed configuration options. For example, like
            // here we can omit verbose output from the logs in production
            c.ResponsePayloadLoggingStrategy = isDevelopment
                ? PayloadLoggingStrategy.IndentedJson
                : PayloadLoggingStrategy.Omit;

            // You can also make the logging strategy dependent on the message or response
            // payloads, e.g. to omit confidential data from the logs
            c.ResponsePayloadLoggingStrategyFactory = (_, resp) =>
                resp.Exists(m => m.CounterName == "confidential")
                    ? PayloadLoggingStrategy.Omit
                    : c.ResponsePayloadLoggingStrategy;

            // you can customize logging even further by hooking into the log message creation
            c.PostExecutionHook = ctx =>
            {
                if (ctx.Response.Exists(m => m.CounterName == "confidential"))
                {
                    // log an additional explanation for why the response is omitted from the logs
                    ctx.Logger.LogInformation("response omitted because of confidential data");
                }

                return true; // let the default message be logged
            };
        });

    public async Task<List<CounterValue>> Handle(
        GetCounters message,
        CancellationToken cancellationToken = default
    )
    {
        var allCounters = await repository.GetCounters();

        return allCounters
            .Where(p => message.Prefix is null || p.Key.StartsWith(message.Prefix))
            .Select(p => new CounterValue(p.Key, p.Value))
            .ToList();
    }
}
```

> <details>
> <summary>Click here to see a more realistic trimmed down version of the file</summary>
>
> [GetCountersHandler.cs](examples/quickstart.enhanced/GetCountersHandler.cs)
>
> <!-- REPLACECODE examples/quickstart.enhanced/GetCountersHandler.cs -->
> ```cs
> namespace Quickstart.Enhanced;
> 
> internal sealed partial class GetCountersHandler(CountersRepository repository)
>     : GetCounters.IHandler
> {
>     public static void ConfigurePipeline(GetCounters.IPipeline pipeline) =>
>         pipeline
>             .UseDefault()
>             .OmitResponsePayloadFromLogsInProduction()
>             .OmitResponsePayloadFromLogsForResponseMatching(r =>
>                 r.Exists(c => c.CounterName == "confidential")
>             );
> 
>     public async Task<List<CounterValue>> Handle(
>         GetCounters message,
>         CancellationToken cancellationToken = default
>     )
>     {
>         var allCounters = await repository.GetCounters();
> 
>         return allCounters
>             .Where(p => message.Prefix is null || p.Key.StartsWith(message.Prefix))
>             .Select(p => new CounterValue(p.Key, p.Value))
>             .ToList();
>     }
> }
> ```
>
> </details>

Finally, set up the app in [Program.cs](examples/quickstart/Program.cs):

<!-- REPLACECODE examples/quickstart/Program.cs -->
```cs
using Quickstart;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddSingleton<CountersRepository>()
    // This registers all the handlers in the project; alternatively, you can register
    // individual handlers as well
    .AddMessageHandlersFromAssembly(typeof(Program).Assembly)
    .AddSignalHandlersFromAssembly(typeof(Program).Assembly)
    // Add services that Conqueror needs to properly expose things via HTTP
    .AddConquerorHttpServerAspNetCore()
    // Let's also enable Swashbuckle to get a nice Swagger UI
    .AddSwaggerGen();

var app = builder.Build();

app.UseSwagger().UseSwaggerUI();

// This enables message handlers as minimal HTTP API endpoints (including in AOT mode
// if you need that, although please check the corresponding recipe for more details)
app.MapMessageEndpoints();

// This adds a minimal API endpoint which allows consuming published signals
// via Server-Sent Events
app.MapServerSentEventsSignalsEndpoint("api/signals/sse");

await app.RunAsync();
```

Now launch your app:

<!-- REPLACECODE examples/quickstart/run.sh -->
```sh
dotnet run
```

And then you can call the message handlers via HTTP.

<!-- REPLACECODE examples/quickstart/call.sh -->
```sh
curl http://localhost:5000/api/v1/incrementCounterByAmount \
  --data '{"counterName":"test","incrementBy":2}' \
  -H 'Content-Type: application/json'
# prints {"newCounterValue":2}

curl http://localhost:5000/api/v1/getCounters?prefix=tes
# prints [{"counterName":"test","value":2}]

# this doubles the increment operation through a signal handler
curl http://localhost:5000/api/v1/incrementCounterByAmount \
  --data '{"counterName":"doubler","incrementBy":2}' \
  -H 'Content-Type: application/json'
# prints {"newCounterValue":4}

curl http://localhost:5000/api/v1/getCounters
# prints [{"counterName":"test","value":2},{"counterName":"doubler","value":4}]

# add a confidential counter
curl http://localhost:5000/api/v1/incrementCounterByAmount \
  --data '{"counterName":"confidential","incrementBy":1000}' \
  -H 'Content-Type: application/json'
# prints {"newCounterValue":1000}

curl http://localhost:5000/api/v1/getCounters
# prints [{"counterName":"test","value":2},{"counterName":"doubler","value":4},{"counterName":"confidential","value":1000}]
```

You can also observe signals via SSE by launching the following in a separate shell:

<!-- REPLACECODE examples/quickstart/call-sse.sh -->
```sh
curl http://localhost:5000/api/signals/sse?signalEventType=counterIncremented

# follow the above by this in another shell to see the signal:
# curl http://localhost:5000/api/v1/incrementCounterByAmount \
# --data '{"counterName":"sseTest","incrementBy":2}' \
# -H 'Content-Type: application/json'

# this prints something like this on the SSE stream:
# event: counterIncremented
# data: {"counterName":"sseTest","newValue":2,"incrementBy":2}
# data: d|conqueror-message-id:5227cf9ead99f26b|trace-id:b107e7cb47d8951996339d99baf4cd28
# id: 2d98b4aca7ee02df
```

Thanks to the logging middleware we added to the pipelines, you will see output similar to this in the server console.

> Are you able to spot a bug in our logging configuration for confidential counters?

<!-- REPLACECODE examples/quickstart/run.log -->
```log
info: Quickstart.IncrementCounterByAmountHandler[711195907]
      Handling http message of type 'IncrementCounterByAmount' with payload
      {
        "CounterName": "test",
        "IncrementBy": 2
      }
      (Message ID: 3e525a72131960dd, Trace ID: ad0871fcc6bb2aabef62f2b24ab5b27c)
info: Quickstart.CounterIncremented[441733974]
      Publishing in-process signal of type 'CounterIncremented' with payload {"CounterName":"test","NewValue":2,"IncrementBy":2} (Signal ID: e24899ea8053616a, Trace ID: ad0871fcc6bb2aabef62f2b24ab5b27c)
info: Quickstart.CounterIncremented[1977864143]
      Published in-process signal of type 'CounterIncremented' in 18.6353ms (Signal ID: e24899ea8053616a, Trace ID: ad0871fcc6bb2aabef62f2b24ab5b27c)
info: Quickstart.CounterIncremented[441733974]
      Publishing http-server-sent-events signal of type 'CounterIncremented' with payload {"CounterName":"test","NewValue":2,"IncrementBy":2} (Signal ID: 0ae709d709480a49, Trace ID: ad0871fcc6bb2aabef62f2b24ab5b27c)
info: Quickstart.CounterIncremented[1977864143]
      Published http-server-sent-events signal of type 'CounterIncremented' in 5.5543ms (Signal ID: 0ae709d709480a49, Trace ID: ad0871fcc6bb2aabef62f2b24ab5b27c)
info: Quickstart.IncrementCounterByAmountHandler[412531951]
      Handled http message of type 'IncrementCounterByAmount' and got response {"NewCounterValue":2} in 74.4882ms (Message ID: 3e525a72131960dd, Trace ID: ad0871fcc6bb2aabef62f2b24ab5b27c)
info: Quickstart.GetCountersHandler[711195907]
      Handling http message of type 'GetCounters' with payload {"Prefix":"tes"} (Message ID: a8b8870794d685ac, Trace ID: 47572a64d30dfdd48b1d4f9c28810343)
info: Quickstart.GetCountersHandler[412531951]
      Handled http message of type 'GetCounters' and got response
      [
        {
          "CounterName": "test",
          "Value": 2
        }
      ]
      in 9.4573ms (Message ID: a8b8870794d685ac, Trace ID: 47572a64d30dfdd48b1d4f9c28810343)
info: Quickstart.IncrementCounterByAmountHandler[711195907]
      Handling http message of type 'IncrementCounterByAmount' with payload
      {
        "CounterName": "doubler",
        "IncrementBy": 2
      }
      (Message ID: 36b3258481257142, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.CounterIncremented[441733974]
      Publishing in-process signal of type 'CounterIncremented' with payload {"CounterName":"doubler","NewValue":2,"IncrementBy":2} (Signal ID: 60988c4de621536b, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.DoublingCounterIncrementedHandler[441733974]
      Handling signal of type 'CounterIncremented' with payload
      {
        "CounterName": "doubler",
        "NewValue": 2,
        "IncrementBy": 2
      }
      (Signal ID: 60988c4de621536b, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.IncrementCounterByAmount[711195907]
      Sending in-process message of type 'IncrementCounterByAmount' with payload {"CounterName":"doubler","IncrementBy":2} (Message ID: 9c1fc1a4a60859f4, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.IncrementCounterByAmountHandler[711195907]
      Handling message of type 'IncrementCounterByAmount' with payload
      {
        "CounterName": "doubler",
        "IncrementBy": 2
      }
      (Message ID: 9c1fc1a4a60859f4, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.CounterIncremented[441733974]
      Publishing in-process signal of type 'CounterIncremented' with payload {"CounterName":"doubler","NewValue":4,"IncrementBy":2} (Signal ID: a0b26fbbd938941e, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.CounterIncremented[1977864143]
      Published in-process signal of type 'CounterIncremented' in 1.7726ms (Signal ID: a0b26fbbd938941e, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.CounterIncremented[441733974]
      Publishing http-server-sent-events signal of type 'CounterIncremented' with payload {"CounterName":"doubler","NewValue":4,"IncrementBy":2} (Signal ID: 4163f38ccc92aaea, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.CounterIncremented[1977864143]
      Published http-server-sent-events signal of type 'CounterIncremented' in 0.0852ms (Signal ID: 4163f38ccc92aaea, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.IncrementCounterByAmountHandler[412531951]
      Handled message of type 'IncrementCounterByAmount' and got response {"NewCounterValue":4} in 6.5008ms (Message ID: 9c1fc1a4a60859f4, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.IncrementCounterByAmount[412531951]
      Sent in-process message of type 'IncrementCounterByAmount' and got response {"NewCounterValue":4} in 12.6340ms (Message ID: 9c1fc1a4a60859f4, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.DoublingCounterIncrementedHandler[1977864143]
      Handled signal of type 'CounterIncremented' in 28.2218ms (Signal ID: 60988c4de621536b, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.CounterIncremented[1977864143]
      Published in-process signal of type 'CounterIncremented' in 31.8950ms (Signal ID: 60988c4de621536b, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.CounterIncremented[441733974]
      Publishing http-server-sent-events signal of type 'CounterIncremented' with payload {"CounterName":"doubler","NewValue":2,"IncrementBy":2} (Signal ID: 3d9ddb98bc8fb654, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.CounterIncremented[1977864143]
      Published http-server-sent-events signal of type 'CounterIncremented' in 0.3467ms (Signal ID: 3d9ddb98bc8fb654, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.IncrementCounterByAmountHandler[412531951]
      Handled http message of type 'IncrementCounterByAmount' and got response {"NewCounterValue":4} in 33.4722ms (Message ID: 36b3258481257142, Trace ID: e9623fa2087d6ad22d1ddc36ad0e7e11)
info: Quickstart.GetCountersHandler[711195907]
      Handling http message of type 'GetCounters' with payload {"Prefix":null} (Message ID: b533e0b87c07e88d, Trace ID: c4729469ee9bf9597b55ab65105a1c99)
info: Quickstart.GetCountersHandler[412531951]
      Handled http message of type 'GetCounters' and got response
      [
        {
          "CounterName": "test",
          "Value": 2
        },
        {
          "CounterName": "doubler",
          "Value": 4
        }
      ]
      in 0.5002ms (Message ID: b533e0b87c07e88d, Trace ID: c4729469ee9bf9597b55ab65105a1c99)
info: Quickstart.IncrementCounterByAmountHandler[711195907]
      Handling http message of type 'IncrementCounterByAmount' with payload
      {
        "CounterName": "confidential",
        "IncrementBy": 1000
      }
      (Message ID: f5ce2cdef6add70f, Trace ID: 801d949d99273dac26376a77f172c418)
info: Quickstart.CounterIncremented[441733974]
      Publishing in-process signal of type 'CounterIncremented' with payload {"CounterName":"confidential","NewValue":1000,"IncrementBy":1000} (Signal ID: 53afdf09cd14a316, Trace ID: 801d949d99273dac26376a77f172c418)
info: Quickstart.CounterIncremented[1977864143]
      Published in-process signal of type 'CounterIncremented' in 0.1800ms (Signal ID: 53afdf09cd14a316, Trace ID: 801d949d99273dac26376a77f172c418)
info: Quickstart.CounterIncremented[441733974]
      Publishing http-server-sent-events signal of type 'CounterIncremented' with payload {"CounterName":"confidential","NewValue":1000,"IncrementBy":1000} (Signal ID: 45d34058c9921758, Trace ID: 801d949d99273dac26376a77f172c418)
info: Quickstart.CounterIncremented[1977864143]
      Published http-server-sent-events signal of type 'CounterIncremented' in 0.0776ms (Signal ID: 45d34058c9921758, Trace ID: 801d949d99273dac26376a77f172c418)
info: Quickstart.IncrementCounterByAmountHandler[412531951]
      Handled http message of type 'IncrementCounterByAmount' and got response {"NewCounterValue":1000} in 1.0167ms (Message ID: f5ce2cdef6add70f, Trace ID: 801d949d99273dac26376a77f172c418)
info: Quickstart.GetCountersHandler[711195907]
      Handling http message of type 'GetCounters' with payload {"Prefix":null} (Message ID: 6f859893a3442f1e, Trace ID: a06963c6f5ed5e77d3d0646df4afc748)
info: Quickstart.GetCountersHandler[0]
      response omitted because of confidential data
info: Quickstart.GetCountersHandler[412531951]
      Handled http message of type 'GetCounters' in 0.7302ms (Message ID: 6f859893a3442f1e, Trace ID: a06963c6f5ed5e77d3d0646df4afc748)
info: Quickstart.IncrementCounterByAmountHandler[711195907]
      Handling http message of type 'IncrementCounterByAmount' with payload
      {
        "CounterName": "sseTest",
        "IncrementBy": 2
      }
      (Message ID: 5227cf9ead99f26b, Trace ID: b107e7cb47d8951996339d99baf4cd28)
info: Quickstart.CounterIncremented[441733974]
      Publishing in-process signal of type 'CounterIncremented' with payload {"CounterName":"sseTest","NewValue":2,"IncrementBy":2} (Signal ID: 2aa6b849d45cef9f, Trace ID: b107e7cb47d8951996339d99baf4cd28)
info: Quickstart.CounterIncremented[1977864143]
      Published in-process signal of type 'CounterIncremented' in 0.1912ms (Signal ID: 2aa6b849d45cef9f, Trace ID: b107e7cb47d8951996339d99baf4cd28)
info: Quickstart.CounterIncremented[441733974]
      Publishing http-server-sent-events signal of type 'CounterIncremented' with payload {"CounterName":"sseTest","NewValue":2,"IncrementBy":2} (Signal ID: 2d98b4aca7ee02df, Trace ID: b107e7cb47d8951996339d99baf4cd28)
info: Quickstart.CounterIncremented[1977864143]
      Published http-server-sent-events signal of type 'CounterIncremented' in 8.6999ms (Signal ID: 2d98b4aca7ee02df, Trace ID: b107e7cb47d8951996339d99baf4cd28)
info: Quickstart.IncrementCounterByAmountHandler[412531951]
      Handled http message of type 'IncrementCounterByAmount' and got response {"NewCounterValue":2} in 9.7013ms (Message ID: 5227cf9ead99f26b, Trace ID: b107e7cb47d8951996339d99baf4cd28)
```
<!-- 
If you have swagger UI enabled, it will show the new messages and they can be called from there.

<!-
  use an HTML image instead of a markdown image to ensure that enough
  vertical space is reserved even before the image is loaded so that
  links to anchors in the readme work correctly
->
<img src="./examples/quickstart/swagger.gif?raw=true" alt="Quickstart Swagger" style="height: 565px" height="565px" /> -->

## Libraries

[![NuGet version (Conqueror)](https://img.shields.io/nuget/v/Conqueror?label=Conqueror)](https://www.nuget.org/packages/Conqueror/)
[![NuGet version (Conqueror.Abstractions)](https://img.shields.io/nuget/v/Conqueror.Abstractions?label=Conqueror.Abstractions)](https://www.nuget.org/packages/Conqueror.Abstractions/)

### Middlewares

[![NuGet version (Conqueror.Middleware.Authorization)](https://img.shields.io/nuget/v/Conqueror.Middleware.Authorization?label=Conqueror.Middleware.Authorization)](https://www.nuget.org/packages/Conqueror.Middleware.Authorization/)
[![NuGet version (Conqueror.Middleware.Logging)](https://img.shields.io/nuget/v/Conqueror.Middleware.Logging?label=Conqueror.Middleware.Logging)](https://www.nuget.org/packages/Conqueror.Middleware.Logging/)

### Transports

[![NuGet version (Conqueror.Transport.Http.Abstractions)](https://img.shields.io/nuget/v/Conqueror.Transport.Http.Abstractions?label=Conqueror.Transport.Http.Abstractions)](https://www.nuget.org/packages/Conqueror.Transport.Http.Abstractions/)
[![NuGet version (Conqueror.Transport.Http.Client)](https://img.shields.io/nuget/v/Conqueror.Transport.Http.Client?label=Conqueror.Transport.Http.Client)](https://www.nuget.org/packages/Conqueror.Transport.Http.Client/)
[![NuGet version (Conqueror.Transport.Http.Server.AspNetCore)](https://img.shields.io/nuget/v/Conqueror.Transport.Http.Server.AspNetCore?label=Conqueror.Transport.Http.Server.AspNetCore)](https://www.nuget.org/packages/Conqueror.Transport.Http.Server.AspNetCore/)

## Functionalities

<details>
<summary>Click here to see documentation still under construction</summary>

### **Messaging**

[![status-stable](https://img.shields.io/badge/status-stable-brightgreen)](https://www.nuget.org/packages/Conqueror/)

Split your business processes into simple-to-maintain and easy-to-test pieces of code using the [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation) pattern. Handle cross-cutting concerns like logging, validation, authorization etc. using configurable middlewares. Keep your applications scalable by moving commands and queries from a modular monolith to a distributed application with minimal friction.

Head over to our [recipes](#recipes) for more guidance on how to use this library.

### **Signalling**

[![status-stable](https://img.shields.io/badge/status-stable-yellow)](https://www.nuget.org/packages/Conqueror/)

Decouple your application logic by using in-process signal publishing using the [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) pattern. Handle cross-cutting concerns like logging, tracing, filtering etc. using configurable middlewares. Keep your applications scalable by moving signals from a modular monolith to a distributed application with minimal friction.

Head over to our [signalling recipes](#signalling-introduction) for more guidance on how to use this library.

### Experimental Functionalities

The functionalities below are still experimental. This means they do not have a stable API and are missing code documentation and recipes. They are therefore not suited for use in production applications, but can be used in proofs-of-concept or toy apps. If you use any of the experimental libraries and find bugs or have ideas for improving them, please don't hesitate to [create an issue](https://github.com/MrWolfZ/Conqueror/issues/new).

### **Iterating**

[![status-experimental](https://img.shields.io/badge/status-experimental-yellow)](https://www.nuget.org/packages/Conqueror/)

Keep your applications in control by allowing them to consume [data streams](https://en.wikipedia.org/wiki/Data_stream) at their own pace using a pull-based approach. Handle cross-cutting concerns like logging, error handling, authorization etc. using configurable middlewares. Keep your applications scalable by moving stream consumers from a modular monolith to a distributed application with minimal friction.

Head over to our [iterating recipes](#iterating-introduction) for more guidance on how to use this library.

</details>

## Recipes

In addition to code-level API documentation, **Conqueror** provides you with recipes that will guide you in how to utilize it to its maximum. Each recipe will help you solve one particular challenge that you will likely encounter while building a .NET application.

> For every "How do I do X?" you can imagine for this project, you should be able to find a recipe here. If you don't see a recipe for your question, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new) or even better, provide the recipe as a pull request.

### Messaging Introduction

<details>
<summary>Click here to see documentation still under construction</summary>

CQS is an acronym for [command-query separation](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation) (which is the inspiration for this project and also where the name is derived from: conquer -> **co**mmands a**n**d **quer**ies). The core idea behind this pattern is that operations which only read data (i.e. queries) and operations which mutate data or cause side-effects (i.e. commands) have very different characteristics (for a start, in most applications queries are executed much more frequently than commands). In addition, business operations often map very well to commands and queries, allowing you to model your application in a way that allows technical and business stakeholders alike to understand the capabilities of the system. There are many other benefits we gain from following this separation in our application logic. For example, commands and queries represent a natural boundary for encapsulation, provide clear contracts for modularization, and allow solving cross-cutting concerns according to the nature of the operation (e.g. caching makes sense for queries, but not so much for commands). With commands and queries, testing often becomes more simple as well, since they provide a clear list of the capabilities that should be tested (allowing more focus to be placed on use-case-driven testing instead of traditional unit testing).

#### Messaging Basics

- [getting started](src/core/recipes/messaging/getting-started#readme)
- [testing handlers](src/core/recipes/messaging/testing-handlers#readme)
- [solving cross-cutting concerns with middlewares (e.g. validation or retrying on failure)](src/core/recipes/messaging/solving-cross-cutting-concerns#readme)
- [testing handlers that have middleware pipelines](src/core/recipes/messaging/testing-handlers-with-pipelines#readme)
- [testing middlewares and reusable pipelines](src/core/recipes/messaging/testing-middlewares#readme)

#### Messaging Advanced

- [exposing messages via HTTP](src/transports/http/recipes/messaging/exposing-via-http#readme)
- [testing HTTP messages](src/transports/http/recipes/messaging/testing-http#readme)
- [calling HTTP messages from another application](src/transports/http/recipes/messaging/calling-http#readme)
- [testing code which calls HTTP messages](src/transports/http/recipes/messaging/testing-calling-http#readme)
- [creating a clean architecture and modular monolith with messages](src/core/recipes/messaging/clean-architecture#readme)
- [moving from a modular monolith to a distributed system](src/core/recipes/messaging/monolith-to-distributed#readme)
- using a different dependency injection container (e.g. Autofac or Ninject) _(to-be-written)_
- customizing OpenAPI specification for HTTP messages _(to-be-written)_
- re-use middleware pipelines to solve cross-cutting concerns when calling external systems (e.g. logging or retrying failed calls) _(to-be-written)_
<!-- 
- enforce that all message handlers declare a pipeline _(to-be-written)_
- using messages in a Blazor app (server-side or web-assembly) _(to-be-written)_
- building a CLI using messages _(to-be-written)_
-->

#### Messaging Expert

- store and access background context information in the scope of a single message _(to-be-written)_
- propagate background context information (e.g. trace ID) across multiple messages, signals, and iterators _(to-be-written)_
- accessing properties of messages in middlewares _(to-be-written)_
- exposing and calling messages via other transports (e.g. gRPC) _(to-be-written)_

#### Messaging Cross-Cutting Concerns

- authenticating and authorizing messages _(to-be-written)_
- logging messages _(to-be-written)_
- validating messages _(to-be-written)_
- caching message results for improved performance _(to-be-written)_
- making messages more resilient (e.g. through retries, circuit breakers, fallbacks etc.) _(to-be-written)_
- executing messages in a database transaction _(to-be-written)_
- timeouts for messages _(to-be-written)_
- metrics for messages _(to-be-written)_
- tracing messages _(to-be-written)_

</details>

### Recipes for experimental functionalities

<details>
<summary>Click here to see recipes for experimental functionalities</summary>

### Signalling Introduction

[![library-status-experimental](https://img.shields.io/badge/library%20status-experimental-yellow)](https://www.nuget.org/packages/Conqueror/)

Signalling is a way to refer to the publishing and observing of signals via the [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) pattern. Signalling is a good way to decouple or loosely couple different parts of your application by making a signal publisher agnostic to the observers of signals it publishes. In addition to this basic idea, **Conqueror** allows solving cross-cutting concerns on both the publisher as well as the observer side.

#### Signalling Basics

- [getting started](src/core/recipes/signalling/getting-started#readme)
- [testing signal handlers](src/core/recipes/signalling/testing-handlers#readme)
- testing code that publishes signals _(to-be-written)_
- solving cross-cutting concerns with middlewares (e.g. logging or retrying on failure) _(to-be-written)_
- testing signal handlers with pipelines _(to-be-written)_
- testing signal publisher pipeline _(to-be-written)_
- testing middlewares _(to-be-written)_

#### Signalling Advanced

- using a different dependency injection container (e.g. Autofac or Ninject) _(to-be-written)_
- execute signal handlers with a different strategy (e.g. parallel execution) _(to-be-written)_
- enforce that all signal handlers declare a pipeline _(to-be-written)_
- creating a clean architecture with loose coupling via signals _(to-be-written)_
- moving from a modular monolith to a distributed system _(to-be-written)_

#### Signalling Expert

- store and access background context information in the scope of a single signal _(to-be-written)_
- propagate background context information (e.g. trace ID) across multiple messages, signals, and iterators _(to-be-written)_
- accessing properties of signals in middlewares _(to-be-written)_

#### Signalling Cross-Cutting Concerns

- logging signals _(to-be-written)_
- retrying failed signal handlers _(to-be-written)_
- executing signal handlers in a database transaction _(to-be-written)_
- metrics for signals _(to-be-written)_
- tracing signals _(to-be-written)_

### Iterating Introduction

[![library-status-experimental](https://img.shields.io/badge/library%20status-experimental-yellow)](https://www.nuget.org/packages/Conqueror/)

For [data streaming](https://en.wikipedia.org/wiki/Data_stream) **Conqueror** uses a pull-based approach where the consumer controls the pace (using `IAsyncEnumerable`), which is a good approach for use cases like paging and event sourcing.

#### Iterating Basics

- [getting started](src/core/recipes/iterating/getting-started#readme)
- testing iterator handlers _(to-be-written)_
- solving cross-cutting concerns with middlewares (e.g. validation or retrying on failure) _(to-be-written)_
- testing iterator handlers that have middleware pipelines _(to-be-written)_
- testing middlewares _(to-be-written)_

#### Iterating Advanced

- using a different dependency injection container (e.g. Autofac or Ninject) _(to-be-written)_
- reading iterators from a messaging system (e.g. Kafka or RabbitMQ) _(to-be-written)_
- exposing iterators via HTTP _(to-be-written)_
- testing HTTP iterators _(to-be-written)_
- consuming HTTP iterators from another application _(to-be-written)_
- using middlewares for iterator HTTP clients _(to-be-written)_
- optimize HTTP iterating performance with pre-fetching _(to-be-written)_
- enforce that all iterator handlers declare a pipeline _(to-be-written)_
- re-use middleware pipelines to solve cross-cutting concerns when consuming iterators from external systems (e.g. logging or retrying failed calls) _(to-be-written)_
- authenticating and authorizing iterators _(to-be-written)_
- moving from a modular monolith to a distributed system _(to-be-written)_

#### Iterating Expert

- store and access background context information in the scope of a single iterator _(to-be-written)_
- propagate background context information (e.g. trace ID) across multiple messages, signals, and iterators _(to-be-written)_
- accessing properties of iterators in middlewares _(to-be-written)_
- exposing and consuming iterators via other transports (e.g. SignalR) _(to-be-written)_
- building test assertions that work for HTTP and non-HTTP iterators _(to-be-written)_

#### Iterating Cross-Cutting Concerns

- authenticating and authorizing iterators _(to-be-written)_
- logging iterators and items _(to-be-written)_
- validating iterators _(to-be-written)_
- retrying failed iterators _(to-be-written)_
- timeouts for iterators and items _(to-be-written)_
- metrics for iterators and items _(to-be-written)_
- tracing iterators and items _(to-be-written)_

</details>

## Motivation

Modern software development is often centered around building web applications that communicate via [HTTP](https://en.wikipedia.org/wiki/Hypertext_Transfer_Protocol) (we'll call them "web APIs"). However, many applications require different entry points or APIs as well (e.g. message queues, command line interfaces, raw TCP or UDP sockets, etc.). Each of these kinds of APIs need to address a variety of cross-cutting concerns, most of which apply to all kinds of APIs (e.g. logging, tracing, error handling, authorization, etc.). Microsoft has done an excellent job in providing out-of-the-box solutions for many of these concerns when building web APIs with [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core) using [middlewares](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-7.0) (which implement the [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) pattern). However, for other kinds of APIs, development teams are often forced to handle these concerns themselves, spending valuable development time.

One way many teams choose to address this issue is by forcing every operation to go through a web API (e.g. having a small adapter that reads messages from a queue and then calls a web API for processing the message). While this works well in many cases, it adds extra complexity and fragility by adding a new integration point for very little value. Optimally, there would be a way to address the cross-cutting concerns in a consistent way for all kinds of APIs. This is exactly what **Conqueror** does. It provides the building blocks for implementing business functionality and addressing those cross-cutting concerns in an transport-agnostic fashion, and provides extension packages that allow exposing the business functionality via different transports (e.g. HTTP).

A useful side-effect of moving the handling of cross-cutting concerns away from the concrete transport, is that it allows solving cross-cutting concerns for both incoming and outgoing operations. For example, with **Conqueror** the exact same code can be used for adding retry capabilities for your own command and query handlers as well as when calling an external HTTP API.

On an architectural level, a popular way to build systems these days is using [microservices](https://microservices.io). While microservices are a powerful approach, they can often represent a significant challenge for small or new teams, mostly for deployment and operations (challenges common to most [distributed systems](https://en.wikipedia.org/wiki/Distributed_computing)). A different approach that many teams choose is to start with a [modular monolith](https://martinfowler.com/bliki/MonolithFirst.html) and move to microservices at a later point. However, it is common for teams to struggle with such a migration, partly due to sub-optimal modularization and partly due to existing tools and libraries not providing a smooth transition journey from one approach to another (or often forcing you into the distributed approach directly, e.g. [MassTransit](https://masstransit-project.com)). **Conqueror** addresses this by encouraging you to build modules with clearly defined contracts and by allowing you to switch from having a module be part of a monolith to be its own microservice with minimal code changes.

**Conqueror** leverages design patterns like [messaging](https://en.wikipedia.org/wiki/Messaging_pattern), [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) (often also known as _middlewares_), [aspect-oriented programming](https://en.wikipedia.org/wiki/Aspect-oriented_programming), [builder pattern](https://en.wikipedia.org/wiki/Builder_pattern), [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern), and more.

In summary, these are some of the strengths of **Conqueror**:

- **Providing building blocks for many different communication patterns:** Many applications require the use of different communication patterns to fulfill their business requirements (e.g. `request-response`, `fire-and-forget`, `publish-subscribe`, `streaming` etc.). **Conqueror** provides building blocks for implementing these communication patterns efficiently and consistently, while allowing you to address cross-cutting concerns in a transport-agnostic fashion.

- **Excellent use-case-driven documentation:** A lot of effort went into writing our [recipes](#recipes). While most other libraries have documentation that is centered around explaining _what_ they do, our use-case-driven documentation is focused on showing you how **Conqueror** _helps you to solve the concrete challenges_ your are likely to encounter during application development.

- **Strong focus on testability:** Testing is a very important topic that is sadly often neglected. **Conqueror** takes testability very seriously and makes sure that you know how you can test the code you have written using it (you may have noticed that the **Conqueror.CQS** recipe immediately following [getting started](src/core/recipes/messaging/getting-started#readme) shows you how you can [test the handlers](src/core/recipes/messaging/testing-handlers#readme) we built in the first recipe).

- **Out-of-the-box solutions for many common yet often complex cross-cutting concerns:** Many development teams spend valuable time on solving common cross-cutting concerns like validation, logging, error handling etc. over and over again. **Conqueror** provides a variety of pre-built middlewares that help you address those concerns with minimal effort.

- **Migrating from a modular monolith to a distributed system with minimal friction:** Business logic built on top of **Conqueror** provides clear contracts to consumers, regardless of whether these consumers are located in the same process or in a different application. By abstracting away the concrete transport over which the business logic is called, it can easily be moved from a monolithic approach to a distributed approach with minimal code changes.

- **Modular and extensible architecture:** Instead of a big single library, **Conqueror** consists of many small (independent or complementary) packages. This allows you to pick and choose what functionality you want to use without adding the extra complexity for anything that you don't. It also improves maintainability by allowing modifications and extensions with a lower risk of breaking any existing functionality (in addition to a high level of public-API-focused test coverage).

### Comparison with similar projects

Below you can find a brief comparison with some popular projects which address similar concerns as **Conqueror**.

#### Differences to MediatR

The excellent library [MediatR](https://github.com/jbogard/MediatR) is a popular choice for building applications. **Conqueror** takes a lot of inspirations from its design, with some key differences:

- MediatR allows handling cross-cutting concerns with global behaviors, while **Conqueror** allows handling these concerns with composable middlewares in independent pipelines per handler type.
- MediatR uses a single message sender service which makes it tricky to navigate to a message handler in your IDE from the point where the message is sent. With **Conqueror** you call handlers through an explicit interface, allowing you to use the "Go to implementation" functionality of your IDE.
- MediatR is focused building single applications without any support for any transports, while **Conqueror** allows building both single applications as well as distributed systems that communicate via different transports implemented through adapters.

#### Differences to MassTransit

[MassTransit](https://masstransit-project.com) is a great framework for building distributed applications. It addresses many of the same concerns as **Conqueror**, with some key differences:

- MassTransit is designed for building distributed systems, forcing you into this approach from the start, even if you don't need it yet (the provided in-memory transport is explicitly mentioned as not being recommended for production usage). **Conqueror** allows building both single applications as well as distributed systems.
- MassTransit is focused on asynchronous messaging, while **Conqueror** provides more communication patterns (e.g. synchronous request-response over HTTP).
- MassTransit has adapters for many messaging middlewares, like RabbitMQ or Azure Service Bus, which **Conqueror** does not.
- MassTransit provides out-of-the-box solutions for advanced patterns like sagas, state machines, etc., which **Conqueror** does not.

If you require the advanced patterns or messaging middleware connectors which MassTransit provides, you can easily combine it with **Conqueror** by calling command and query handlers from your consumers or wrapping your producers in command handlers.
