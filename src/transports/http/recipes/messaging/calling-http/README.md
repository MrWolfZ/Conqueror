# Conqueror recipe (Messaging): calling HTTP messages from another application

This recipe shows how simple it is to call your HTTP messages from another application with **Conqueror**.

This is an advanced recipe which builds upon the concepts introduced in the [recipes about messaging basics](../../../../../..#messaging-basics) as well as the recipe for [exposing messages via HTTP](../exposing-via-http#readme). If you have not yet read those recipes, we recommend you take a look at them before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/transports/http/recipes/messaging/calling-http) and open the solution file in your IDE (note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/transports/http/recipes/messaging/calling-http).

The application, for which we will be calling HTTP messages, is managing a set of named counters. In code, the API of our application is represented with the following types:

```cs
[HttpMessage<IncrementCounterResponse>(Version = "v1")]
public partial record IncrementCounter(string CounterName);

public record IncrementCounterResponse(int NewCounterValue);

[HttpMessage<GetCounterValueResponse>(HttpMethod = "GET", Version = "v1")]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);
```

The server application is already [set up to expose these messages via HTTP](Conqueror.Recipes.Messaging.CallingHttp.Server/Program.cs). Feel free to take a look at the full code for [incrementing a counter](Conqueror.Recipes.Messaging.CallingHttp.Server/IncrementCounter.cs) and [getting a counter's value](Conqueror.Recipes.Messaging.CallingHttp.Server/GetCounterValue.cs). The counters are stored in an [in-memory repository](Conqueror.Recipes.Messaging.CallingHttp.Server/CountersRepository.cs).

The client application, from which we are going to call the messages, is a command line application (but everything would work exactly the same if, for example, the client were a microservice calling messages from another microservice). We've already prepared the core application logic in the client's [Program.cs](Conqueror.Recipes.Messaging.CallingHttp.Client/Program.cs) to be ready for calling the server.

After we're done implementing the client, we will be able to use it like this:

```txt
> dotnet run inc test
incremented counter 'test'; new value: 1
> dotnet run get test
counter 'test' value: 1
```

It is possible to implement the client application with plain HTTP calls sent from an `HttpClient`. This works, but it forces you to manually handle various aspects of HTTP that are not truly relevant for achieving what we want (e.g. you would need to manually construct the target URI and serialize the payload). **Conqueror** provides a better way, which abstracts the details of HTTP (or any other transport) away from your application logic, so that your code becomes simpler and more reusable.

With **Conqueror** you send a message through an `IMessageSenders` instance and choose a _transport_ at the call site. One such transport is HTTP. The awesome thing about this is that the details of the transport are handled in the background and your code simply calls the message like it would without HTTP. For example, the `IncrementCounter` message would be sent like this:

```cs
var response = await senders.For(IncrementCounter.T)
                            .WithTransport(b => b.UseHttp(serverAddress))
                            .Handle(new IncrementCounter(counterName));
```

This means your code is perfectly type-safe and can work with and without HTTP or any other transport. It all just depends on which transport you configure when sending the message. Before we look at how this works, there is one thing we need to do in preparation. As you may have noticed above, we use the `IncrementCounter` message type when sending the message. We want to do this in our client command line app, but currently this type is defined inside our server web application. We could just add a project reference from the client project to the server project, but that would cause a code dependency which we do not want. Instead, the recommended approach is to extract your HTTP message types into a separate shared class library, which is typically called `Contracts` or `DataTransferObjects`. Let's create such a project and add references to it from the client and server applications:

```sh
dotnet new classlib -n Conqueror.Recipes.Messaging.CallingHttp.Contracts
dotnet sln Conqueror.Recipes.Messaging.CallingHttp.sln add Conqueror.Recipes.Messaging.CallingHttp.Contracts
dotnet add Conqueror.Recipes.Messaging.CallingHttp.Client reference Conqueror.Recipes.Messaging.CallingHttp.Contracts
dotnet add Conqueror.Recipes.Messaging.CallingHttp.Server reference Conqueror.Recipes.Messaging.CallingHttp.Contracts
```

We also need to add a package reference to the contracts to allow using the `HttpMessage` attribute (this package also brings the **Conqueror** source generator, which creates the required metadata for our message types):

```sh
dotnet add Conqueror.Recipes.Messaging.CallingHttp.Contracts package Conqueror.Transport.Http.Abstractions
```

Now we can move the message types into the new project. Let's start by creating a new file `IncrementCounter.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.CallingHttp.Contracts/IncrementCounter.cs)) in the contracts project:

```cs
namespace Conqueror.Recipes.Messaging.CallingHttp.Contracts;

[HttpMessage<IncrementCounterResponse>(Version = "v1")]
public partial record IncrementCounter(string CounterName)
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "counter name must not be empty")]
    public string CounterName { get; } = CounterName;
}

public record IncrementCounterResponse(int NewCounterValue);
```

Next, create a new file `GetCounterValue.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.CallingHttp.Contracts/GetCounterValue.cs)) in the contracts project:

```cs
namespace Conqueror.Recipes.Messaging.CallingHttp.Contracts;

[HttpMessage<GetCounterValueResponse>(HttpMethod = "GET", Version = "v1")]
public partial record GetCounterValue(string CounterName)
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "counter name must not be empty")]
    public string CounterName { get; } = CounterName;
}

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);
```

> The `[Required]` attribute is a [data annotation](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations) that we will use further down below to validate the message before sending it. We place it in the contracts project so that both the client and server can enforce the same rules.

If you have not yet done so, you can now remove the message types from the server application code and reference the types from the contracts project instead. To reduce the noise in your code you may want to add the `Conqueror.Recipes.Messaging.CallingHttp.Contracts` namespace as a global using statement in [Usings.cs](.completed/Conqueror.Recipes.Messaging.CallingHttp.Server/Usings.cs).

With this new contracts project we can now start sending messages from our client application. Let's start by adding a new package:

```sh
dotnet add Conqueror.Recipes.Messaging.CallingHttp.Client package Conqueror.Transport.Http.Client
```

Next, we need to add some configuration in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.CallingHttp.Client/Program.cs)). Calling `AddConquerorHttpClient` registers the services which the HTTP transport requires (including the `IMessageSenders` we will use for sending messages):

```diff
+ using Conqueror;
+ using Conqueror.Recipes.Messaging.CallingHttp.Contracts;
+
+ // in a real application this would be loaded from some configuration source
+ var serverAddress = new Uri("http://localhost:5000");
+
  var services = new ServiceCollection();

+ services.AddConquerorHttpClient();
+
  await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

+ var senders = serviceProvider.GetRequiredService<IMessageSenders>();
```

With this change in place we can start sending messages. Let's send the `IncrementCounter` message in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.CallingHttp.Client/Program.cs)). We resolve the message sender via `senders.For`, configure it to use the HTTP transport pointing at our server, and call `Handle`:

```diff
  case "inc":
+     var incResponse = await senders.For(IncrementCounter.T)
+                                    .WithTransport(b => b.UseHttp(serverAddress))
+                                    .Handle(new(counterName));
+     Console.WriteLine($"incremented counter '{counterName}'; new value: {incResponse.NewCounterValue}");
      break;
```

Now you can launch the server application (in your IDE or a separate shell) and use the client like this:

```txt
> dotnet run inc test --project Conqueror.Recipes.Messaging.CallingHttp.Client
incremented counter 'test'; new value: 1
> dotnet run inc test --project Conqueror.Recipes.Messaging.CallingHttp.Client
incremented counter 'test'; new value: 2
```

Let's do the same for the `GetCounterValue` message in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.CallingHttp.Client/Program.cs)):

```diff
  case "get":
+     var getResponse = await senders.For(GetCounterValue.T)
+                                    .WithTransport(b => b.UseHttp(serverAddress))
+                                    .Handle(new(counterName));
+     Console.WriteLine(getResponse.CounterExists
+                           ? $"counter '{counterName}' value: {getResponse.CounterValue}"
+                           : $"counter '{counterName}' does not exist");
      break;
```

> `UseHttp` is only available for message types that are decorated with the `HttpMessage` attribute. If you need to call a custom endpoint that is not exposed through the attribute, you need to use a plain `HttpClient` instead.

If you run the increment command a few times you will find that it sometimes fails with an unhandled exception. This is due to the server simulating instability by making requests fail every once in a while. The unhandled exception is of type [HttpMessageFailedOnClientException](../../../Conqueror.Transport.Http.Abstractions/Messaging/HttpMessageFailedOnClientException.cs), which is the exception type that **Conqueror** throws if the HTTP call fails for any reason. The exception contains the HTTP status code and full response message to allow you to decide how to deal with the failure. Let's add some handling for such failures to `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.CallingHttp.Client/Program.cs)):

```diff
      }
  }
+ catch (HttpMessageFailedOnClientException httpException)
+ {
+     Console.WriteLine($"HTTP message failed with status code {(int?)httpException.StatusCode}");
+ }
  catch (ValidationException validationException)
  {
      Console.WriteLine(validationException.Message);
  }
```

Now the transient failures are reported cleanly:

```txt
> dotnet run inc test --project Conqueror.Recipes.Messaging.CallingHttp.Client
HTTP message failed with status code 500
```

Having to deal with an HTTP-specific exception like this is one aspect of **Conqueror**'s transport approach which cannot be fully abstracted away. However, depending on your error handling strategy, you may still be able to keep most of your code transport-agnostic, for example by handling these exceptions as part of your top-level application error handling or by handling them with middlewares as discussed further down below.

The code we wrote above covers the most basic possible HTTP client setup. There are various ways to customize the behavior of the HTTP transport, which we'll look at next. The first, and most common customization that you may require for your HTTP clients, is to set custom HTTP headers (for example to provide authentication data). This can be configured on the transport builder with `WithHeaders`:

```cs
await senders.For(IncrementCounter.T)
             .WithTransport(b => b.UseHttp(serverAddress)
                                  .WithHeaders(h => h.Add("my-header", "my-value")))
             .Handle(new(counterName));
```

The transport builder exposes the `IServiceProvider` (via `b.ServiceProvider`), so you can resolve any services you need while configuring the transport, for example to fetch an authentication token.

If you have multiple messages that all target the same server with the same settings, repeating the transport configuration at every call site becomes tedious and error-prone. The recommended practice is to create a custom extension method on the `MessageSenderBuilder`. Let's create such an extension in a new file `CounterServerTransportExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.CallingHttp.Client/CounterServerTransportExtensions.cs)):

```cs
namespace Conqueror.Recipes.Messaging.CallingHttp.Client;

internal static class CounterServerTransportExtensions
{
    // in a real application this would be loaded from some configuration source,
    // e.g. by resolving IConfiguration from builder.ServiceProvider
    private static readonly Uri ServerAddress = new("http://localhost:5000");

    public static IHttpMessageSender<TMessage, TResponse> UseCounterServer<TMessage, TResponse>(
        this MessageSenderBuilder<TMessage, TResponse> builder)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        return builder.UseHttp(ServerAddress)
                      .WithHeaders(h => h.Add("my-header", "my-value"));
    }
}
```

With this extension method, configuring the transport becomes very simple. We use it for both messages, which lets us drop the `serverAddress` variable in `Program.cs` since the address now lives in the extension:

```diff
  case "inc":
      var incResponse = await senders.For(IncrementCounter.T)
-                                    .WithTransport(b => b.UseHttp(serverAddress))
+                                    .WithTransport(b => b.UseCounterServer())
                                     .Handle(new(counterName));

  case "get":
      var getResponse = await senders.For(GetCounterValue.T)
-                                    .WithTransport(b => b.UseHttp(serverAddress))
+                                    .WithTransport(b => b.UseCounterServer())
                                     .Handle(new(counterName));
```

> This is the transport-configuration analog of a strongly-typed client: instead of registering a dedicated client type for each message ahead of time, the transport is composed at the call site, and shared configuration is captured in a reusable extension method that works for any HTTP message.

The final feature we are going to look at are middlewares. From the recipe for [solving cross-cutting concerns](../../../../../core/recipes/messaging/solving-cross-cutting-concerns#readme) you may recall that each message handler can have a middleware pipeline that is executed as part of its handling. The same applies when sending a message: you can attach a pipeline at the call site with `WithPipeline`. This allows handling cross-cutting concerns not only on the server, but also on the client, before the message is sent over the wire.

To see how this can be useful, let's send an increment without providing a counter name:

```txt
> dotnet run inc --project Conqueror.Recipes.Messaging.CallingHttp.Client
HTTP message failed with status code 500
```

The empty counter name was sent over the network and only failed on the server (the server runs the same message pipeline and rejects the empty name). We wasted a network round-trip on a message that was never going to succeed. Instead, we can validate the message on the client before we send the HTTP request. The server already has a custom [DataAnnotationValidationMiddleware](Conqueror.Recipes.Messaging.CallingHttp.Server/DataAnnotationValidationMiddleware.cs) which we can use for this. The recommended approach to make your middlewares usable in both servers and clients is to extract them into a separate shared class library, which is typically called `Middlewares`. Let's create such a project and add references to it from the client and server applications:

```sh
dotnet new classlib -n Conqueror.Recipes.Messaging.CallingHttp.Middlewares
dotnet sln Conqueror.Recipes.Messaging.CallingHttp.sln add Conqueror.Recipes.Messaging.CallingHttp.Middlewares
dotnet add Conqueror.Recipes.Messaging.CallingHttp.Client reference Conqueror.Recipes.Messaging.CallingHttp.Middlewares
dotnet add Conqueror.Recipes.Messaging.CallingHttp.Server reference Conqueror.Recipes.Messaging.CallingHttp.Middlewares
```

> Note that **Conqueror** provides [pre-built middlewares](../../../../../..#recipes) for the most common cross-cutting concerns (e.g. [Conqueror.Middleware.Logging](https://www.nuget.org/packages/Conqueror.Middleware.Logging) or [Conqueror.Middleware.Polly](https://www.nuget.org/packages/Conqueror.Middleware.Polly)). If those are sufficient for your use case, you don't need to create middlewares yourself and can simply add a package reference to the middleware package in both client and server.

We also need to add a package reference to the new project to allow using **Conqueror** types like the `IMessageMiddleware` interface:

```sh
dotnet add Conqueror.Recipes.Messaging.CallingHttp.Middlewares package Conqueror.Abstractions
```

Now we can move the `DataAnnotationValidationMiddleware.cs` file and the accompanying `DataAnnotationValidationMiddlewarePipelineExtensions.cs` file to the new project ([view completed middleware](.completed/Conqueror.Recipes.Messaging.CallingHttp.Middlewares/DataAnnotationValidationMiddleware.cs) and [extension](.completed/Conqueror.Recipes.Messaging.CallingHttp.Middlewares/DataAnnotationValidationMiddlewarePipelineExtensions.cs)). The middleware simply validates the message according to its data annotation attributes before executing the rest of the pipeline:

```cs
namespace Conqueror.Recipes.Messaging.CallingHttp.Middlewares;

public class DataAnnotationValidationMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        // this will validate the object according to data annotation attributes and
        // will throw a ValidationException if validation fails
        Validator.ValidateObject(ctx.Message, new ValidationContext(ctx.Message), validateAllProperties: true);

        // if validation passes, execute the rest of the pipeline
        return ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}
```

Since the middleware now lives in a shared project, both the server and the client can use it. On the server, the handler continues to configure it in its pipeline (with the namespace now pointing at the shared project). In the client we attach it to the sender's pipeline via `WithPipeline` in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.CallingHttp.Client/Program.cs)):

```diff
  case "inc":
      var incResponse = await senders.For(IncrementCounter.T)
+                                    .WithPipeline(p => p.UseDataAnnotationValidation())
                                     .WithTransport(b => b.UseCounterServer())
                                     .Handle(new(counterName));

  case "get":
      var getResponse = await senders.For(GetCounterValue.T)
+                                    .WithPipeline(p => p.UseDataAnnotationValidation())
                                     .WithTransport(b => b.UseCounterServer())
                                     .Handle(new(counterName));
```

If you now run the application with invalid input as before, you get a better error message and no HTTP request is sent at all:

```txt
> dotnet run inc --project Conqueror.Recipes.Messaging.CallingHttp.Client
counter name must not be empty
```

That looks much better. Being able to use the same middlewares in both server and clients is one of the best features of **Conqueror**. In addition to validation, there are many other cross-cutting concerns which you might want to handle on both client and server, including logging, retrying failed calls, caching results, etc. Take a look at our recipe for [solving cross-cutting concerns](../../../../../core/recipes/messaging/solving-cross-cutting-concerns#readme) for more inspiration of what is possible with this approach.

Similar to the reusable pipelines we created in the recipe for [solving cross-cutting concerns](../../../../../core/recipes/messaging/solving-cross-cutting-concerns#readme), it can be useful to create a shared default pipeline for your client-side message sends. For example, you could create an extension method that ensures all outgoing messages are validated and retried on failure:

```cs
public static IMessagePipeline<TMessage, TResponse> UseClientDefault<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
    where TMessage : class, IMessage<TMessage, TResponse>
{
    return pipeline.UseDataAnnotationValidation()
                   .UseRetry();
}
```

With this extension method, sending a message with the default pipeline becomes very simple:

```cs
await senders.For(IncrementCounter.T)
             .WithPipeline(p => p.UseClientDefault())
             .WithTransport(b => b.UseCounterServer())
             .Handle(new(counterName));
```

> Since sender pipelines and handler pipelines use the same builder interface (`IMessagePipeline<TMessage, TResponse>`) and the same underlying mechanism, you can reuse the very same pipeline extension methods in both clients and handlers.

And that concludes this recipe for calling your HTTP messages from another application with **Conqueror**. In summary, you need to do the following:

- add a reference to the [Conqueror.Transport.Http.Client](https://www.nuget.org/packages/Conqueror.Transport.Http.Client/) package to your client application
- add the HTTP client services via `AddConquerorHttpClient`
- extract your HTTP message types into a shared contracts library
- send messages by choosing the HTTP transport at the call site via `senders.For(X.T).WithTransport(b => b.UseHttp(...))`
- capture shared transport configuration in a custom extension method on the `MessageSenderBuilder`
- handle cross-cutting concerns on the client with sender pipelines via `WithPipeline`

As the next step we recommend that you explore how to [test your code which calls HTTP messages](../testing-calling-http#readme) or how to [create a clean architecture with messages](../../../../../core/recipes/messaging/clean-architecture#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.calling-http]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
