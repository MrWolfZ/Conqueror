<!-- markdownlint-disable MD033 -->

# Conqueror - a highly ergonomic library for building structured, scalable .NET apps

**Conqueror** is a .NET library that simplifies writing modular, scalable applications by unifying messages, signals, and more into a consistent, extensible model. It uses modern features of .NET like source generators and static abstract interface methods to reduce boilerplate, support advanced uses cases like AOT compilation, and to provide a highly ergonomic user-friendly API.

Whether you're building a monolith or distributed microservices, **Conqueror** provides a seamless experience with minimal ceremony. It also eases the transition from a modular monolith to a distributed system with minimal friction, giving teams the flexibility to start simple and delay the transition until the right time in a project's lifecycle.

**Conqueror** encourages clean architectures by decoupling your application logic from concrete transports like HTTP, and allows exposing business operations via many different transports with thin adapters.

**Conqueror** leverages design patterns like [messaging](https://en.wikipedia.org/wiki/Messaging_pattern), [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) (often also known as _middlewares_), [aspect-oriented programming](https://en.wikipedia.org/wiki/Aspect-oriented_programming), [builder pattern](https://en.wikipedia.org/wiki/Builder_pattern), [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern), and more.

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

This quickstart guide will let you jump right into the code without lengthy explanations. If you prefer more guidance, head over to our [recipes](#recipes). By following this quickstart guide, you'll add HTTP messages and an in-process signal to a minimal API ASP.NET Core application. You can also find the [source code](recipes/quickstart) here in the repository.

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
using System.ComponentModel.DataAnnotations;
using Conqueror;

namespace Quickstart;

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
    [Range(1, long.MaxValue)]
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
[Signal]
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
> using System.ComponentModel.DataAnnotations;
> using Conqueror;
> 
> namespace Quickstart.Enhanced;
> 
> [HttpMessage<CounterIncrementedResponse>(Version = "v1")]
> public sealed partial record IncrementCounterByAmount(string CounterName)
> {
>     [Range(1, long.MaxValue)]
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
> [Signal]
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
using System.Collections.Concurrent;

namespace Quickstart;

// simulate a database repository (which is usually async)
internal sealed class CountersRepository
{
    private readonly ConcurrentDictionary<string, long> counters = new();

    public async Task<long> AddOrIncrementCounter(string counterName, long incrementBy)
    {
        await Task.Yield();
        return counters.AddOrUpdate(counterName, incrementBy, (_, value) => value + incrementBy);
    }

    public async Task<long> GetCounterValue(string counterName)
    {
        await Task.Yield();
        return counters.GetValueOrDefault(counterName, 0L);
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
using System.ComponentModel.DataAnnotations;
using Conqueror;

namespace Quickstart;

// The handler type is also enhanced by the Conqueror source generator, so it must be partial
internal sealed partial class IncrementCounterByAmountHandler(
        CountersRepository repository,
        ISignalPublishers publishers)

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
                Validator.ValidateObject(ctx.Message, new(ctx.Message), true);

                // The middleware has access to the message with its proper type (i.e. the
                // compiler knows that `ctx.Message` is of type `IncrementCounterByAmount`),
                // so you could also write the validation directly like this:
                if (ctx.Message.IncrementBy <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(ctx.Message.IncrementBy),
                        "increment amount must be positive");
                }

                return ctx.Next(ctx.Message, ctx.CancellationToken);
            })

            // The `Use...` methods add middlewares to the pipeline, and afterward they can be
            // configured further, which is useful for extracting common configuration into a
            // shared method and then configure it per handler, e.g. like this:
            // `pipeline.UseDefault().WithIndentedJsonPayloadLogFormatting()`
            .ConfigureLogging(c => c.MessagePayloadLoggingStrategy =
                                  PayloadLoggingStrategy.IndentedJson);

    public async Task<CounterIncrementedResponse> Handle(
        IncrementCounterByAmount message,
        CancellationToken cancellationToken = default)
    {
        var newValue = await repository.AddOrIncrementCounter(
            message.CounterName,
            message.IncrementBy);

        // `ISignalPublishers` is a factory to get a publisher for a signal type. The
        // `CounterIncremented.T` property is generated by the source generator and is used for
        // type inference. The 'For' method returns a `CounterIncremented.IHandler` (which is a
        // proxy used to publish the signal)
        await publishers.For(CounterIncremented.T)

                        // When publishing a signal (or sending a message, etc.), you can also
                        // specify a pipeline, which is executed before the transport is called.
                        // And when the transport delivers the payload, the handler's own pipeline
                        // is executed as well
                        .WithPipeline(p => p.UseLogging())

                        // You can customize the transport which is used to publish the signal
                        // (e.g. publishing it via RabbitMQ), but here we configure the in-process
                        // transport to use parallel broadcasting for demonstration (instead of
                        // the default sequential broadcasting). You can also pass your own custom
                        // strategy if you need it
                        .WithTransport(b => b.UseInProcessWithParallelBroadcastingStrategy())

                        // The 'Handle' method is unique for each `IHandler`. This means that your
                        // IDE's "Go to Implementation" feature will show all signal handlers for
                        // this signal, making it simple to find all the places in your code where
                        // a signal is used
                        .Handle(
                            new(message.CounterName, newValue, message.IncrementBy),
                            cancellationToken);

        return new(await repository.GetCounterValue(message.CounterName));
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
> using Conqueror;
> 
> namespace Quickstart.Enhanced;
> 
> internal sealed partial class IncrementCounterByAmountHandler(
>     CountersRepository repository,
>     ISignalPublishers publishers)
>     : IncrementCounterByAmount.IHandler
> {
>     public static void ConfigurePipeline(IncrementCounterByAmount.IPipeline pipeline) =>
>         pipeline.UseDefault();
> 
>     public async Task<CounterIncrementedResponse> Handle(
>         IncrementCounterByAmount message,
>         CancellationToken cancellationToken = default)
>     {
>         var newValue = await repository.AddOrIncrementCounter(message.CounterName,
>                                                               message.IncrementBy);
> 
>         await publishers.For(CounterIncremented.T)
>                         .WithDefaultPublisherPipeline(typeof(IncrementCounterByAmountHandler))
>                         .Handle(new(message.CounterName, newValue, message.IncrementBy),
>                                 cancellationToken);
> 
>         return new(await repository.GetCounterValue(message.CounterName));
>     }
> }
> ```
>
> </details>

In [DoublingCounterIncrementedHandler.cs](examples/quickstart/DoublingCounterIncrementedHandler.cs) create a signal handler that doubles increment operations on specific counters.

<!-- REPLACECODE examples/quickstart/DoublingCounterIncrementedHandler.cs -->
```cs
using Conqueror;

namespace Quickstart;

internal sealed partial class DoublingCounterIncrementedHandler(
    IMessageSenders senders)
    : CounterIncremented.IHandler
{
    // Signal handlers support handling multiple signal types (by adding more `IHandler`
    // interfaces), so the pipeline configuration is generic and is reused for all signal types
    // (`typeof(T)` can be checked to customize the pipeline for a specific signal type)
    static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline) =>
        pipeline.Use(ctx =>
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
                    if (ctx.ConquerorContext.ContextData.Get<bool>("doubled"))
                    {
                        return Task.CompletedTask;
                    }

                    ctx.ConquerorContext.ContextData.Set("doubled", true);

                    return ctx.Next(ctx.Signal, ctx.CancellationToken);
                })

                // Middlewares in the pipeline are executed in the order that they are added.
                // We add the logging middleware to the pipeline only after the prior two
                // middlewares to ensure that only signals which are not skipped get logged
                .UseLogging(o => o.PayloadLoggingStrategy = PayloadLoggingStrategy.IndentedJson);

    public async Task Handle(
        CounterIncremented signal,
        CancellationToken cancellationToken = default)
    {
        await senders
              .For(IncrementCounterByAmount.T)

              // Message senders can also have pipelines and use different transports. The exact
              // same middlewares like logging, validation, error handling, etc. can be used on
              // both senders/publishers and handlers
              .WithPipeline(p => p.UseLogging())
              .WithTransport(b => b.UseInProcess())

              // The 'Handle' method is unique for each `IHandler`, so "Go to Implementation" in
              // your IDE will jump directly to your handler, enabling smooth code base navigation,
              // even across different projects and transports
              .Handle(
                  new(signal.CounterName) { IncrementBy = signal.IncrementBy },
                  cancellationToken);
    }
}
```

> <details>
> <summary>Click here to see a more realistic trimmed down version of the file</summary>
>
> [DoublingCounterIncrementedHandler.cs](examples/quickstart.enhanced/DoublingCounterIncrementedHandler.cs)
>
> <!-- REPLACECODE examples/quickstart.enhanced/DoublingCounterIncrementedHandler.cs -->
> ```cs
> using Conqueror;
> 
> namespace Quickstart.Enhanced;
> 
> internal sealed partial class DoublingCounterIncrementedHandler(
>     IMessageSenders senders)
>     : CounterIncremented.IHandler
> {
>     static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline) =>
>         pipeline.SkipSignalMatching((CounterIncremented s) => s.CounterName != "doubler")
>                 .EnsureSingleExecutionPerOperation(nameof(DoublingCounterIncrementedHandler))
>                 .UseDefault();
> 
>     public async Task Handle(
>         CounterIncremented signal,
>         CancellationToken cancellationToken = default)
>     {
>         await senders.For(IncrementCounterByAmount.T)
>                      .WithDefaultSenderPipeline(typeof(DoublingCounterIncrementedHandler))
>                      .Handle(new(signal.CounterName) { IncrementBy = signal.IncrementBy },
>                              cancellationToken);
>     }
> }
> ```
>
> </details>

In [GetCountersHandler.cs](examples/quickstart/GetCountersHandler.cs) create a message handler that returns a filtered list of counters.

<!-- REPLACECODE examples/quickstart/GetCountersHandler.cs -->
```cs
using Conqueror;

namespace Quickstart;

internal sealed partial class GetCountersHandler(
    CountersRepository repository)
    : GetCounters.IHandler
{
    public static void ConfigurePipeline(GetCounters.IPipeline pipeline) =>
        pipeline.UseLogging(c =>
        {
            // The pipeline has access to the service provider from the scope of the call to the
            // handler in case you need it to resolve some services
            var isDevelopment = pipeline.ServiceProvider
                                        .GetRequiredService<IHostEnvironment>()
                                        .IsDevelopment();

            // The logging middleware supports detailed configuration options. For example, like
            // here we can omit verbose output from the logs in production
            c.ResponsePayloadLoggingStrategy = isDevelopment
                ? PayloadLoggingStrategy.IndentedJson
                : PayloadLoggingStrategy.Omit;

            // You can also make the logging strategy dependent on the message or response
            // payloads, e.g. to omit confidential data from the logs
            c.ResponsePayloadLoggingStrategyFactory = (_, resp)
                => resp.Any(m => m.CounterName == "confidential")
                    ? PayloadLoggingStrategy.Omit
                    : c.ResponsePayloadLoggingStrategy;

            // you can customize logging even further by hooking into the log message creation
            c.PostExecutionHook = ctx =>
            {
                if (ctx.Response.Any(m => m.CounterName == "confidential"))
                {
                    // log an additional explanation for why the response is omitted from the logs
                    ctx.Logger.LogInformation("response omitted because of confidential data");
                }

                return true; // let the default message be logged
            };
        });

    public async Task<List<CounterValue>> Handle(
        GetCounters message,
        CancellationToken cancellationToken = default)
    {
        var allCounters = await repository.GetCounters();

        return allCounters.Where(p => message.Prefix is null || p.Key.StartsWith(message.Prefix))
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
> internal sealed partial class GetCountersHandler(
>     CountersRepository repository)
>     : GetCounters.IHandler
> {
>     public static void ConfigurePipeline(GetCounters.IPipeline pipeline) =>
>         pipeline.UseDefault()
>                 .OmitResponsePayloadFromLogsInProduction()
>                 .OmitResponsePayloadFromLogsForResponseMatching(r => r.Any(c => c.CounterName ==
>                                                                         "confidential"));
> 
>     public async Task<List<CounterValue>> Handle(
>         GetCounters message,
>         CancellationToken cancellationToken = default)
>     {
>         var allCounters = await repository.GetCounters();
> 
>         return allCounters.Where(p => message.Prefix is null || p.Key.StartsWith(message.Prefix))
>                           .Select(p => new CounterValue(p.Key, p.Value))
>                           .ToList();
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

builder.Services
       .AddSingleton<CountersRepository>()

       // This registers all the handlers in the project; alternatively, you can register
       // individual handlers as well
       .AddMessageHandlersFromAssembly(typeof(Program).Assembly)
       .AddSignalHandlersFromAssembly(typeof(Program).Assembly)

       // Add some services that Conqueror needs to properly expose messages via HTTP
       .AddMessageEndpoints()

       // Let's enable Swashbuckle to get a nice Swagger UI
       .AddSwaggerGen();

var app = builder.Build();

app.UseSwagger()
   .UseSwaggerUI();

// This enables message handlers as minimal HTTP API endpoints (including in AOT mode
// if you need that, although please check the corresponding recipe for more details)
app.MapMessageEndpoints();

app.Run();
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
      (Message ID: 8d6593393b592be7, Trace ID: 9f49b02534df157313aa4fe5edc36bfe)
info: Quickstart.IncrementCounterByAmountHandler[412531951]
      Handled http message of type 'IncrementCounterByAmount' and got response {"NewCounterValue":2} in 33.2277ms (Message ID: 8d6593393b592be7, Trace ID: 9f49b02534df157313aa4fe5edc36bfe)
info: Quickstart.GetCountersHandler[711195907]
      Handling http message of type 'GetCounters' with payload {"Prefix":"tes"} (Message ID: 8c1ef6a764aed796, Trace ID: a804d8d086b2102afa2af651ad86fb36)
info: Quickstart.GetCountersHandler[412531951]
      Handled http message of type 'GetCounters' and got response
      [
        {
          "CounterName": "test",
          "Value": 2
        }
      ]
      in 6.3624ms (Message ID: 8c1ef6a764aed796, Trace ID: a804d8d086b2102afa2af651ad86fb36)
info: Quickstart.IncrementCounterByAmountHandler[711195907]
      Handling http message of type 'IncrementCounterByAmount' with payload
      {
        "CounterName": "doubler",
        "IncrementBy": 2
      }
      (Message ID: 2f2cb247021d2ee0, Trace ID: acc51361d87c132744e4d1ac40b31f46)
info: Quickstart.DoublingCounterIncrementedHandler[441733974]
      Handling signal of type 'CounterIncremented' with payload
      {
        "CounterName": "doubler",
        "NewValue": 2,
        "IncrementBy": 2
      }
      (Signal ID: 85d8955727f526aa, Trace ID: acc51361d87c132744e4d1ac40b31f46)
info: Quickstart.IncrementCounterByAmount[0]
      doubling increment of counter 'doubler'
info: Quickstart.IncrementCounterByAmountHandler[711195907]
      Handling message of type 'IncrementCounterByAmount' with payload
      {
        "CounterName": "doubler",
        "IncrementBy": 2
      }
      (Message ID: 5311de941ad7aa20, Trace ID: acc51361d87c132744e4d1ac40b31f46)
info: Quickstart.IncrementCounterByAmountHandler[412531951]
      Handled message of type 'IncrementCounterByAmount' and got response {"NewCounterValue":4} in 3.8204ms (Message ID: 5311de941ad7aa20, Trace ID: acc51361d87c132744e4d1ac40b31f46)
info: Quickstart.IncrementCounterByAmount[0]
      doubled increment of counter 'doubler', it is now 4
info: Quickstart.DoublingCounterIncrementedHandler[1977864143]
      Handled signal of type 'CounterIncremented' in 16.7602ms (Signal ID: 85d8955727f526aa, Trace ID: acc51361d87c132744e4d1ac40b31f46)
info: Quickstart.IncrementCounterByAmountHandler[412531951]
      Handled http message of type 'IncrementCounterByAmount' and got response {"NewCounterValue":4} in 20.9651ms (Message ID: 2f2cb247021d2ee0, Trace ID: acc51361d87c132744e4d1ac40b31f46)
info: Quickstart.GetCountersHandler[711195907]
      Handling http message of type 'GetCounters' with payload {"Prefix":null} (Message ID: 411b31c9614b284e, Trace ID: f1b75fe47e043cbd1ac400d65ce91cb5)
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
      in 0.4262ms (Message ID: 411b31c9614b284e, Trace ID: f1b75fe47e043cbd1ac400d65ce91cb5)
info: Quickstart.IncrementCounterByAmountHandler[711195907]
      Handling http message of type 'IncrementCounterByAmount' with payload
      {
        "CounterName": "confidential",
        "IncrementBy": 1000
      }
      (Message ID: 77884562dab999b1, Trace ID: 7fdf00fc3ef39a8076d0c48c9c545aef)
info: Quickstart.IncrementCounterByAmountHandler[412531951]
      Handled http message of type 'IncrementCounterByAmount' and got response {"NewCounterValue":1000} in 0.6406ms (Message ID: 77884562dab999b1, Trace ID: 7fdf00fc3ef39a8076d0c48c9c545aef)
info: Quickstart.GetCountersHandler[711195907]
      Handling http message of type 'GetCounters' with payload {"Prefix":null} (Message ID: 85791577e3f50c87, Trace ID: 1e9560273e354aa7870dd1da736f44b8)
info: Quickstart.GetCountersHandler[412531951]
      Handled http message of type 'GetCounters' in 0.5875ms (Message ID: 85791577e3f50c87, Trace ID: 1e9560273e354aa7870dd1da736f44b8)
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

- [getting started](recipes/cqs/basics/getting-started#readme)
- [testing command and query handlers](recipes/cqs/basics/testing-handlers#readme)
- [solving cross-cutting concerns with middlewares (e.g. validation or retrying on failure)](recipes/cqs/basics/solving-cross-cutting-concerns#readme)
- [testing command and query handlers that have middleware pipelines](recipes/cqs/basics/testing-handlers-with-pipelines#readme)
- [testing middlewares and reusable pipelines](recipes/cqs/basics/testing-middlewares#readme)

#### Messaging Advanced

- [exposing commands and queries via HTTP](recipes/cqs/advanced/exposing-via-http#readme)
- [testing HTTP commands and queries](recipes/cqs/advanced/testing-http#readme)
- [calling HTTP commands and queries from another application](recipes/cqs/advanced/calling-http#readme)
- [testing code which calls HTTP commands and queries](recipes/cqs/advanced/testing-calling-http#readme)
- [creating a clean architecture and modular monolith with commands and queries](recipes/cqs/advanced/clean-architecture#readme)
- [moving from a modular monolith to a distributed system](recipes/cqs/advanced/monolith-to-distributed#readme)
- [using a different dependency injection container (e.g. Autofac or Ninject)](recipes/cqs/advanced/different-dependency-injection#readme) _(to-be-written)_
- [customizing OpenAPI specification for HTTP commands and queries](recipes/cqs/advanced/custom-openapi-http#readme) _(to-be-written)_
- [re-use middleware pipelines to solve cross-cutting concerns when calling external systems (e.g. logging or retrying failed calls)](recipes/cqs/advanced/reuse-piplines-for-external-calls#readme) _(to-be-written)_
<!-- 
- [enforce that all command and query handlers declare a pipeline](recipes/cqs/advanced/enforce-handler-pipeline#readme) _(to-be-written)_
- [using commands and queries in a Blazor app (server-side or web-assembly)](recipes/cqs/advanced/blazor-server#readme) _(to-be-written)_
- [building a CLI using commands and queries](recipes/cqs/advanced/building-cli#readme) _(to-be-written)_
-->

#### Messaging Expert

- [store and access background context information in the scope of a single command or query](recipes/cqs/expert/command-query-context#readme) _(to-be-written)_
- [propagate background context information (e.g. trace ID) across multiple commands, queries, events, and streams](recipes/cqs/expert/conqueror-context#readme) _(to-be-written)_
- [accessing properties of commands and queries in middlewares](recipes/cqs/expert/accessing-properties-in-middlewares#readme) _(to-be-written)_
- [exposing and calling commands and queries via other transports (e.g. gRPC)](recipes/cqs/expert/exposing-via-other-transports#readme) _(to-be-written)_

#### Messaging Cross-Cutting Concerns

- [authenticating and authorizing commands and queries](recipes/cqs/cross-cutting-concerns/auth#readme) _(to-be-written)_
- [logging commands and queries](recipes/cqs/cross-cutting-concerns/logging#readme) _(to-be-written)_
- [validating commands and queries](recipes/cqs/cross-cutting-concerns/validation#readme) _(to-be-written)_
- [caching query results for improved performance](recipes/cqs/cross-cutting-concerns/caching#readme) _(to-be-written)_
- [making commands and queries more resilient (e.g. through retries, circuit breakers, fallbacks etc.)](recipes/cqs/cross-cutting-concerns/resiliency#readme) _(to-be-written)_
- [executing commands and queries in a database transaction](recipes/cqs/cross-cutting-concerns/db-transaction#readme) _(to-be-written)_
- [timeouts for commands and queries](recipes/cqs/cross-cutting-concerns/timeouts#readme) _(to-be-written)_
- [metrics for commands and queries](recipes/cqs/cross-cutting-concerns/metrics#readme) _(to-be-written)_
- [tracing commands and queries](recipes/cqs/cross-cutting-concerns/tracing#readme) _(to-be-written)_

</details>

### Recipes for experimental functionalities

<details>
<summary>Click here to see recipes for experimental functionalities</summary>

### Signalling Introduction

[![library-status-experimental](https://img.shields.io/badge/library%20status-experimental-yellow)](https://www.nuget.org/packages/Conqueror/)

Signalling is a way to refer to the publishing and observing of signals via the [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) pattern. Signalling is a good way to decouple or loosely couple different parts of your application by making an event publisher agnostic to the observers of signals it publishes. In addition to this basic idea, **Conqueror** allows solving cross-cutting concerns on both the publisher as well as the observer side.

#### Signalling Basics

- [getting started](recipes/eventing/basics/getting-started#readme) _(to-be-written)_
- [testing event observers](recipes/eventing/basics/testing-observers#readme) _(to-be-written)_
- [testing code that publishes events](recipes/eventing/basics/testing-publish#readme) _(to-be-written)_
- [solving cross-cutting concerns with middlewares (e.g. logging or retrying on failure)](recipes/eventing/basics/solving-cross-cutting-concerns#readme) _(to-be-written)_
- [testing event observers with pipelines](recipes/eventing/basics/testing-observers-with-pipelines#readme) _(to-be-written)_
- [testing event publisher pipeline](recipes/eventing/basics/testing-publisher-pipeline#readme) _(to-be-written)_
- [testing middlewares](recipes/eventing/basics/testing-middlewares#readme) _(to-be-written)_

#### Signalling Advanced

- [using a different dependency injection container (e.g. Autofac or Ninject)](recipes/eventing/advanced/different-dependency-injection#readme) _(to-be-written)_
- [execute event observers with a different strategy (e.g. parallel execution)](recipes/eventing/advanced/publishing-strategy#readme) _(to-be-written)_
- [enforce that all event observers declare a pipeline](recipes/eventing/advanced/enforce-observer-pipeline#readme) _(to-be-written)_
- [creating a clean architecture with loose coupling via events](recipes/eventing/advanced/clean-architecture#readme) _(to-be-written)_
- [moving from a modular monolith to a distributed system](recipes/eventing/advanced/monolith-to-distributed#readme) _(to-be-written)_

#### Signalling Expert

- [store and access background context information in the scope of a single event](recipes/eventing/expert/event-context#readme) _(to-be-written)_
- [propagate background context information (e.g. trace ID) across multiple commands, queries, events, and streams](recipes/eventing/expert/conqueror-context#readme) _(to-be-written)_
- [accessing properties of events in middlewares](recipes/eventing/expert/accessing-properties-in-middlewares#readme) _(to-be-written)_

#### Signalling Cross-Cutting Concerns

- [logging events](recipes/eventing/cross-cutting-concerns/logging#readme) _(to-be-written)_
- [retrying failed event observers](recipes/eventing/cross-cutting-concerns/retry#readme) _(to-be-written)_
- [executing event observers in a database transaction](recipes/eventing/cross-cutting-concerns/db-transaction#readme) _(to-be-written)_
- [metrics for events](recipes/eventing/cross-cutting-concerns/metrics#readme) _(to-be-written)_
- [tracing events](recipes/eventing/cross-cutting-concerns/tracing#readme) _(to-be-written)_

### Iterating Introduction

[![library-status-experimental](https://img.shields.io/badge/library%20status-experimental-yellow)](https://www.nuget.org/packages/Conqueror/)

For [data streaming](https://en.wikipedia.org/wiki/Data_stream) **Conqueror** uses a pull-based approach where the consumer controls the pace (using `IAsyncEnumerable`), which is a good approach for use cases like paging and event sourcing.

#### Iterating Basics

- [getting started](recipes/streaming/basics/getting-started#readme) _(to-be-written)_
- [testing streaming request handlers](recipes/streaming/basics/testing-handlers#readme) _(to-be-written)_
- [solving cross-cutting concerns with middlewares (e.g. validation or retrying on failure)](recipes/streaming/basics/solving-cross-cutting-concerns#readme) _(to-be-written)_
- [testing streaming request handlers that have middleware pipelines](recipes/streaming/basics/testing-handlers-with-pipelines#readme) _(to-be-written)_
- [testing middlewares](recipes/streaming/basics/testing-middlewares#readme) _(to-be-written)_

#### Iterating Advanced

- [using a different dependency injection container (e.g. Autofac or Ninject)](recipes/streaming/advanced/different-dependency-injection#readme) _(to-be-written)_
- [reading streams from a messaging system (e.g. Kafka or RabbitMQ)](recipes/streaming/advanced/reading-from-messaging-system#readme) _(to-be-written)_
- [exposing streams via HTTP](recipes/streaming/advanced/exposing-via-http#readme) _(to-be-written)_
- [testing HTTP streams](recipes/streaming/advanced/testing-http#readme) _(to-be-written)_
- [consuming HTTP streams from another application](recipes/streaming/advanced/consuming-http#readme) _(to-be-written)_
- [using middlewares for streaming HTTP clients](recipes/streaming/advanced/middlewares-for-http-clients#readme) _(to-be-written)_
- [optimize HTTP streaming performance with pre-fetching](recipes/streaming/advanced/optimize-http-performance#readme) _(to-be-written)_
- [enforce that all streaming request handlers declare a pipeline](recipes/streaming/advanced/enforce-handler-pipeline#readme) _(to-be-written)_
- [re-use middleware pipelines to solve cross-cutting concerns when consuming streams from external systems (e.g. logging or retrying failed calls)](recipes/streaming/advanced/reuse-piplines-for-external-calls#readme) _(to-be-written)_
- [authenticating and authorizing streaming requests](recipes/streaming/advanced/auth#readme) _(to-be-written)_
- [moving from a modular monolith to a distributed system](recipes/streaming/advanced/monolith-to-distributed#readme) _(to-be-written)_

#### Iterating Expert

- [store and access background context information in the scope of a single streaming request](recipes/streaming/expert/streaming-request-context#readme) _(to-be-written)_
- [propagate background context information (e.g. trace ID) across multiple commands, queries, events, and streams](recipes/streaming/expert/conqueror-context#readme) _(to-be-written)_
- [accessing properties of streaming requests in middlewares](recipes/streaming/expert/accessing-properties-in-middlewares#readme) _(to-be-written)_
- [exposing and consuming streams via other transports (e.g. SignalR)](recipes/streaming/expert/exposing-via-other-transports#readme) _(to-be-written)_
- [building test assertions that work for HTTP and non-HTTP streams](recipes/streaming/expert/building-test-assertions-for-http-and-non-http#readme) _(to-be-written)_

#### Iterating Cross-Cutting Concerns

- [authenticating and authorizing streaming requests](recipes/streaming/cross-cutting-concerns/auth#readme) _(to-be-written)_
- [logging streaming requests and items](recipes/streaming/cross-cutting-concerns/logging#readme) _(to-be-written)_
- [validating streaming requests](recipes/streaming/cross-cutting-concerns/validation#readme) _(to-be-written)_
- [retrying failed streaming requests](recipes/streaming/cross-cutting-concerns/retry#readme) _(to-be-written)_
- [timeouts for streaming requests and items](recipes/streaming/cross-cutting-concerns/timeouts#readme) _(to-be-written)_
- [metrics for streaming requests and items](recipes/streaming/cross-cutting-concerns/metrics#readme) _(to-be-written)_
- [tracing streaming requests and items](recipes/streaming/cross-cutting-concerns/tracing#readme) _(to-be-written)_

</details>

## Motivation

Modern software development is often centered around building web applications that communicate via [HTTP](https://en.wikipedia.org/wiki/Hypertext_Transfer_Protocol) (we'll call them "web APIs"). However, many applications require different entry points or APIs as well (e.g. message queues, command line interfaces, raw TCP or UDP sockets, etc.). Each of these kinds of APIs need to address a variety of cross-cutting concerns, most of which apply to all kinds of APIs (e.g. logging, tracing, error handling, authorization, etc.). Microsoft has done an excellent job in providing out-of-the-box solutions for many of these concerns when building web APIs with [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core) using [middlewares](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-7.0) (which implement the [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) pattern). However, for other kinds of APIs, development teams are often forced to handle these concerns themselves, spending valuable development time.

One way many teams choose to address this issue is by forcing every operation to go through a web API (e.g. having a small adapter that reads messages from a queue and then calls a web API for processing the message). While this works well in many cases, it adds extra complexity and fragility by adding a new integration point for very little value. Optimally, there would be a way to address the cross-cutting concerns in a consistent way for all kinds of APIs. This is exactly what **Conqueror** does. It provides the building blocks for implementing business functionality and addressing those cross-cutting concerns in an transport-agnostic fashion, and provides extension packages that allow exposing the business functionality via different transports (e.g. HTTP).

A useful side-effect of moving the handling of cross-cutting concerns away from the concrete transport, is that it allows solving cross-cutting concerns for both incoming and outgoing operations. For example, with **Conqueror** the exact same code can be used for adding retry capabilities for your own command and query handlers as well as when calling an external HTTP API.

On an architectural level, a popular way to build systems these days is using [microservices](https://microservices.io). While microservices are a powerful approach, they can often represent a significant challenge for small or new teams, mostly for deployment and operations (challenges common to most [distributed systems](https://en.wikipedia.org/wiki/Distributed_computing)). A different approach that many teams choose is to start with a [modular monolith](https://martinfowler.com/bliki/MonolithFirst.html) and move to microservices at a later point. However, it is common for teams to struggle with such a migration, partly due to sub-optimal modularization and partly due to existing tools and libraries not providing a smooth transition journey from one approach to another (or often forcing you into the distributed approach directly, e.g. [MassTransit](https://masstransit-project.com)). **Conqueror** addresses this by encouraging you to build modules with clearly defined contracts and by allowing you to switch from having a module be part of a monolith to be its own microservice with minimal code changes.

In summary, these are some of the strengths of **Conqueror**:

- **Providing building blocks for many different communication patterns:** Many applications require the use of different communication patterns to fulfill their business requirements (e.g. `request-response`, `fire-and-forget`, `publish-subscribe`, `streaming` etc.). **Conqueror** provides building blocks for implementing these communication patterns efficiently and consistently, while allowing you to address cross-cutting concerns in a transport-agnostic fashion.

- **Excellent use-case-driven documentation:** A lot of effort went into writing our [recipes](#recipes). While most other libraries have documentation that is centered around explaining _what_ they do, our use-case-driven documentation is focused on showing you how **Conqueror** _helps you to solve the concrete challenges_ your are likely to encounter during application development.

- **Strong focus on testability:** Testing is a very important topic that is sadly often neglected. **Conqueror** takes testability very seriously and makes sure that you know how you can test the code you have written using it (you may have noticed that the **Conqueror.CQS** recipe immediately following [getting started](recipes/cqs/basics/getting-started#readme) shows you how you can [test the handlers](recipes/cqs/basics/testing-handlers#readme) we built in the first recipe).

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
