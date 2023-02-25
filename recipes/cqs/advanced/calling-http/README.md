# Conqueror recipe (CQS Advanced): calling HTTP commands and queries from another application

This recipe shows how simple it is to call your HTTP commands and queries from another application with **Conqueror.CQS**.

This is an advanced recipe which builds upon the concepts introduced in the [recipes about CQS basics](../../../../../..#cqs-basics) as well as the recipe for [exposing commands and queries via HTTP](../exposing-via-http#readme). If you have not yet read those recipes, we recommend you take a look at them before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/calling-http) and open the solution file in your IDE (note that you need to have [.NET 6 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/calling-http).

The application, for which we will be adding HTTP command and query clients, is managing a set of named counters. In code, the API of our application is represented with the following types:

```cs
[HttpCommand(Version = "v1")]
public sealed record IncrementCounterCommand([Required] string CounterName);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

[HttpQuery(Version = "v1")]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);
```

Feel free to take a look at the full code for [the query](Conqueror.Recipes.CQS.Advanced.CallingHttp.Server/GetCounterValueQuery.cs) and [the command](Conqueror.Recipes.CQS.Advanced.CallingHttp.Server/IncrementCounterCommand.cs). The counters are stored in an [in-memory repository](Conqueror.Recipes.CQS.Advanced.CallingHttp.Server/CountersRepository.cs).

The client application, from which are going to call the command and query, is a command line application (but everything would work exactly the same if, for example, the client would be a microservice calling commands and queries from another microservice). We've already prepared the core application logic in the client's [Program.cs](Conqueror.Recipes.CQS.Advanced.CallingHttp.Client/Program.cs) to be ready for calling the server.

After we're done implemeting the client, we will be able to use it like this:

```txt
> cd Conqueror.Recipes.CQS.Advanced.CallingHttp.Client
> dotnet run
input commands in format '<op> <counterName>' (e.g. 'inc test' or 'get test')
available operations: get, inc
> dotnet run inc test
incremented counter 'test'; new value: 1
> dotnet run inc
The CounterName field is required.
```

It is possible to implement the client application with plain HTTP calls sent from an `HttpClient`. This works, but it forces you to manually handle various aspects of HTTP that are not truly relevant for achieving what we want (e.g. you would need to manually specify the target URI). **Conqueror.CQS** provides a better way, which abstracts the details of HTTP (or any other transport like gRPC) away from your application logic, so that your code becomes simpler and more reusable.

With **Conqueror.CQS** you can create command and query clients that use a specific _transport_ to execute the operation. One such transport is HTTP. The awesome thing about these clients is that the details of the transport are handled in the background and your code simply calls the command or query like it would without HTTP. For example, the `IncrementCounterCommand` would be called like this:

```cs
var handler = Resolve<IIncrementCounterCommandHandler>();
var response = await handler.ExecuteCommand(new IncrementCounterCommand(counterName));
```

This means your code is perfectly type-safe and can work with and without HTTP or any other transport. It all just depends on how the `IIncrementCounterCommandHandler` is added to your services. Before we look at how this works for HTTP clients, there is one more thing we need to do in preparation. As you may have noticed above, we were using the `IIncrementCounterCommandHandler` and `IncrementCounterCommand` types when executing the command. We want to do this in our client command line app, but currently these types are defined inside our server web application. We could just add a project reference from the client project to the server project, but that would cause a code dependency which we do not want. Instead, the recommended approach is to extract your HTTP commands and queries into a separate shared class library, which is typically called `Contracts` or `DataTransferObjects`. Let's create such a project and add references to it from the client and server applications:

```sh
dotnet new classlib -n Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts
dotnet sln Conqueror.Recipes.CQS.Advanced.CallingHttp.sln add Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts
dotnet add Conqueror.Recipes.CQS.Advanced.CallingHttp.Client reference Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts
dotnet add Conqueror.Recipes.CQS.Advanced.CallingHttp.Server reference Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts
```

We also need to add a package reference to the contracts to allow using certain **Conqueror.CQS** types like the `HttpCommand` attribute:

```sh
dotnet add Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts package Conqueror.CQS.Transport.Http.Abstractions
```

Now we can move the command and query types into the new project. This also includes the custom handler interfaces we created, since those can be used across both client and server as well. Let's start by creating a new file `IncrementCounterCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts/IncrementCounterCommand.cs)) in the contracts project:

```cs
using System.ComponentModel.DataAnnotations;

namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts;

[HttpCommand(Version = "v1")]
public sealed record IncrementCounterCommand([Required] string CounterName);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

public interface IIncrementCounterCommandHandler : ICommandHandler<IncrementCounterCommand, IncrementCounterCommandResponse>
{
}
```

Next, create a new file `GetCounterValueQuery.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts/GetCounterValueQuery.cs)) in the contracts project:

```cs
using System.ComponentModel.DataAnnotations;

namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts;

[HttpQuery(Version = "v1")]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>
{
}
```

If you have not yet done so, you can now remove the command and query types from the server application code and reference the types from the contracts project instead. To reduce the noise in your code you may want to add the `Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts` namespace as a global using statement in [Usings.cs](Conqueror.Recipes.CQS.Advanced.CallingHttp.Server/Usings.cs).

With this new contracts project we can now start creating HTTP clients in our client application. Let's start by adding a new package:

```sh
dotnet add Conqueror.Recipes.CQS.Advanced.CallingHttp.Client package Conqueror.CQS.Transport.Http.Client
```

Next, we need to add some configuration in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.CallingHttp.Client/Program.cs)):

```cs
services.AddConquerorCQSHttpClientServices()
        .AddConquerorCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress));
```

> A query client can (unsurprisingly) be added via `AddConquerorQueryClient`.

This code is all that is required to create a client based on our custom command handler interface. It configures the client to use the HTTP transport with the address of our server application.

With this change in place we can start using the command. Let's resolve and call the command handler in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.CallingHttp.Client/Program.cs)):

```diff
  case "inc":
+     var incrementHandler = serviceProvider.GetRequiredService<IIncrementCounterCommandHandler>();
+     var incResponse = await incrementHandler.ExecuteCommand(new(counterName));
+     Console.WriteLine($"incremented counter '{counterName}'; new value: {incResponse.NewCounterValue}");
      break;
```

Now you can launch the server application (in your IDE or a separate shell) and use the client like this:

```txt
> dotnet run inc test --project Conqueror.Recipes.CQS.Advanced.CallingHttp.Client
incremented counter 'test'; new value: 1
> dotnet run inc test --project Conqueror.Recipes.CQS.Advanced.CallingHttp.Client
incremented counter 'test'; new value: 2
```

Instead of configuring the client on the services you can also create clients dynamically through a factory. Let's do that to call our query in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.CallingHttp.Client/Program.cs)):

```diff
  case "get":
+     var queryClientFactory = serviceProvider.GetRequiredService<IQueryClientFactory>();
+     var getValueHandler = queryClientFactory.CreateQueryClient<IGetCounterValueQueryHandler>(b => b.UseHttp(serverAddress));
+     var getValueResponse = await getValueHandler.ExecuteQuery(new(counterName));
+     Console.WriteLine($"counter '{counterName}' value: {getValueResponse.CounterValue}");
      break;
```

> Command clients can be dynamically created in the same way via `ICommandClientFactory`.

If you run the increment command a few times you will find that it sometimes fails with an unhandled exception. This is due to the server simulating instability by making requests fail every once in a while. The unhandled exception was of type [HttpCommandFailedException](../../../../src/Conqueror.CQS.Transport.Http.Abstractions/HttpCommandFailedException.cs), which is the exception type that **Conqueror.CQS** throws if the HTTP call fails for any reason (for queries it would be [HttpQueryFailedException](../../../../src/Conqueror.CQS.Transport.Http.Abstractions/HttpQueryFailedException.cs)). The exception contains the HTTP status code and full response message to allow you to decide how to deal with the failure. Let's add some handling for such failures to `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.CallingHttp.Client/Program.cs)):

```diff
      }
  }
+ catch (HttpCommandFailedException commandFailedException)
+ {
+     Console.WriteLine($"HTTP command failed with status code {(int)commandFailedException.StatusCode}");
+ }
+ catch (HttpQueryFailedException queryFailedException)
+ {
+     Console.WriteLine($"HTTP query failed with status code {(int)queryFailedException.StatusCode}");
+ }
  catch (ValidationException vex)
  {
      Console.WriteLine(vex.Message);
  }
```

Having to deal with HTTP-specific exceptions like this is one aspect of **Conqueror**'s command and query client approach which cannot be fully abstracted away. However, depending on your error handling strategy, you may still be able to keep most of your code transport-agnostic, for example by handling these exceptions as part of your top-level application error handling or by handling them with middlewares as discussed further down below.

The code we wrote above covers the most basic possible HTTP client setup. There are various ways to customize the behavior of the HTTP transport, which we'll look at next. The first, and most common customization that you may require for your HTTP clients, is to set custom HTTP headers (for example to provide authentication data). This can be configured when adding the client in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.CallingHttp.Client/Program.cs)):

```diff
  services.AddConquerorCQSHttpClientServices()
-         .AddConquerorCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress));
+         .AddConquerorCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress, o => o.Headers.Add("my-header", "my-value")));
```

The function to configure the client's transport can also be `async` and can access the service provider to resolve any required services. For example, fetching an authentication bearer token from a service could be done like this:

```cs
services.AddConquerorCommandClient<IIncrementCounterCommandHandler>(async b =>
{
    var accessTokenService = b.ServiceProvider.GetRequiredService<IAccessTokenService>();
    var accessToken = await accessTokenService.GetTokenForServer();
    return b.UseHttp(serverAddress, o => o.Headers.Authorization = new("bearer", accessToken));
});
```

> Note that the transport configuration function is called every time the command is executed. For example, in the example above this means that a new `accessToken` would be fetched every time the command is executed. To prevent this, you need to ensure that any service you use during client transport configuration caches its result.

Another thing you may want to provide are custom `JsonSerializerOptions` to control how your commands and queries are serialized. This can be done either per client or globally for all clients (if both are specified, the config per client wins):

```cs
// set your custom options (make sure they are compatible with the settings on the server)
var jsonSerializerOptions = new JsonSerializerOptions();

// configure JSON options for a client
services.AddConquerorCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress, o => o.JsonSerializerOptions = jsonSerializerOptions));

// configure JSON options for a dynamically created client
commandClientFactory.CreateCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress, o => o.JsonSerializerOptions = jsonSerializerOptions));

// configure JSON options for all clients
services.AddConquerorCQSHttpClientServices(o => o.JsonSerializerOptions = jsonSerializerOptions);
```

Lastly, you may recall from the recipe for [exposing commands and queries via HTTP](../exposing-via-http#readme) that you can configure custom path conventions on the server. On the client side these conventions can be set per client or globally for all clients (if both are specified, the config per client wins):

```cs
// create your custom convention (make sure it is the same as on the server)
var customHttpCommandPathConvention = new CustomHttpCommandPathConvention();

// configure JSON options for a client
services.AddConquerorCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress, o => o.PathConvention = customHttpCommandPathConvention));

// configure JSON options for a dynamically created client
commandClientFactory.CreateCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress, o => o.PathConvention = customHttpCommandPathConvention));

// configure JSON options for all clients
services.AddConquerorCQSHttpClientServices(o => o.CommandPathConvention = customHttpCommandPathConvention);
```

If you are using custom path conventions, we recommend that you place them in your `Contracts` project to allow both client and server to use the same code.

> Being able to set a path convention per client can be useful if your application calls command or queries from multiple other applications which each use different conventions (although we recommend that all **Conqueror.CQS** users stick to the default conventions as much as possible).

If you require even more control over how the HTTP call is handled inside the transport, you can provide an `HttpClient` instance instead of just the server's base URL when setting up the command or query client. This can also be combined with Microsoft's [IHttpClientFactory](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests) approach from the [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) package to manage HTTP clients separately. The only requirement for the provided `HttpClient` is that the `BaseAddress` property must be set to the server's base address.

```cs
// create HTTP client using Microsoft.Extensions.Http
services.AddHttpClient("serverClient", httpClient => httpClient.BaseAddress = serverAddress);

services.AddConquerorCommandClient<IIncrementCounterCommandHandler>(b =>
{
    var httpClient = b.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("serverClient");
    return b.UseHttp(httpClient);
});
```

If you don't provide an `HttpClient` when configuring the client, then by default **Conqueror.CQS** creates a new `HttpClient` for each command or query client instance. Alternatively, you can provide a custom HTTP client factory function when adding the HTTP client services:

```cs
services.AddConquerorCQSHttpClientServices(o =>
{
    o.HttpClientFactory = baseAddress =>
    {
        var httpClient = o.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        httpClient.BaseAddress = baseAddress;
        return httpClient;
    };
});
```

The final awesome feature we are going to look at are middlewares. From the recipe for [solving cross-cutting concerns](../../basics/solving-cross-cutting-concerns#readme) you may recall that each command or query handler can have a middleware pipeline that is called as part of its execution. The same applies for command and query clients. This allows handling cross-cutting concerns not only on the server, but also on the client. To see how this can be useful, let's execute our client application as follows:

```txt
> dotnet run inc --project Conqueror.Recipes.CQS.Advanced.CallingHttp.Client
HTTP command failed with status code 400
```

We called the `inc` operation without providing the counter name, which led to a validation failure in the form of an error `400` from the server. Instead of sending the invalid command to the server, we may want to validate the command before we send the HTTP request, in order to prevent unnecessary network traffic. This can be done with a middleware. The server has a custom [DataAnnotationValidationCommandMiddleware](Conqueror.Recipes.CQS.Advanced.CallingHttp.Server/DataAnnotationValidationCommandMiddleware.cs) which we can use for this. The recommended approach to make your middlewares usable in both servers and clients is to extract them into a separate shared class library, which is typically called `Middlewares`. Let's create such a project and add references to it from the client and server applications:

```sh
dotnet new classlib -n Conqueror.Recipes.CQS.Advanced.CallingHttp.Middlewares
dotnet sln Conqueror.Recipes.CQS.Advanced.CallingHttp.sln add Conqueror.Recipes.CQS.Advanced.CallingHttp.Middlewares
dotnet add Conqueror.Recipes.CQS.Advanced.CallingHttp.Client reference Conqueror.Recipes.CQS.Advanced.CallingHttp.Middlewares
dotnet add Conqueror.Recipes.CQS.Advanced.CallingHttp.Server reference Conqueror.Recipes.CQS.Advanced.CallingHttp.Middlewares
```

> Note that **Conqueror.CQS** provides many [pre-built middlewares](../../../../../..#conquerorcqs) for the most common-cross cutting concerns. If those middlewares are sufficient for your use case, you don't need to create middlewares yourself and can simply add a package reference to the middleware package in both client and server and add the middleware to the services in both apps. There are also dedicated recipes for [addressing certain cross-cutting concerns](../../../../README.md#cqs-cross-cutting-concerns), which provide a more detailed explanation on how to use the relevant middlewares.

We also need to add a package reference to the new project to allow using certain **Conqueror.CQS** types like the `ICommandMiddleware` interface:

```sh
dotnet add Conqueror.Recipes.CQS.Advanced.CallingHttp.Middlewares package Conqueror.CQS.Abstractions
```

Now we can move the `DataAnnotationValidationCommandMiddleware.cs` file and the accompanying `DataAnnotationValidationCommandMiddlewarePipelineBuilderExtensions.cs` file to the new project. Since the middleware is now in a separate assembly, we also need to explictly add it to the services in both client and server. In the server's `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.CallingHttp.Server/Program.cs)) make the following change:

```diff
  builder.Services
         .AddSingleton<CountersRepository>()
         .AddConquerorCQSTypesFromExecutingAssembly()
+
+        // add all middlewares from the shared project
+        .AddConquerorCQSTypesFromAssembly(typeof(DataAnnotationValidationCommandMiddleware).Assembly);
```

In the client's `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Advanced.CallingHttp.Client/Program.cs)) make the following change:

```diff
  services.AddConquerorCQSHttpClientServices()
          .AddConquerorCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress, o => o.Headers.Add("my-header", "my-value")))
+
+         // add all middlewares from the shared project
+         .AddConquerorCQSTypesFromAssembly(typeof(DataAnnotationValidationCommandMiddleware).Assembly);
```

Now we can configure our command client to use the middleware in its pipeline:

```diff
  services.AddConquerorCQSHttpClientServices()
-         .AddConquerorCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress, o => o.Headers.Add("my-header", "my-value")))
+         .AddConquerorCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress, o => o.Headers.Add("my-header", "my-value")),
+                                                                     pipeline => pipeline.UseDataAnnotationValidation())
 
          // add all middlewares from the shared project
          .AddConquerorCQSTypesFromAssembly(typeof(DataAnnotationValidationCommandMiddleware).Assembly);
```

If you now run the application with invalid input as before, you get a different better error message:

```txt
> dotnet run inc --project Conqueror.Recipes.CQS.Advanced.CallingHttp.Client
The CounterName field is required.
```

That looks much better. Being able to use the same middlewares in both server and clients is one of the best features of **Conqueror.CQS**. In addition to validation, there are many other cross-cutting concerns which you might want to handle on both client and server, including logging, retrying failed calls, caching results, etc. Take a look at our recipes for [addressing certain cross-cutting concerns](../../../../README.md#cqs-cross-cutting-concerns) for more inspiration of what is possible with this approach.

And that concludes this recipe for calling your HTTP commands and queries from another application with **Conqueror.CQS**. In summary, you need to do the following:

- add a reference to the [Conqueror.CQS.Transport.Http.Client](https://www.nuget.org/packages/Conqueror.CQS.Transport.Http.Client/) package to your client application
- add HTTP client services via `AddConquerorCQSHttpClientServices` and optionally configure them with a custom HTTP client factory etc.
- extract your command, query, and custom handler interface types into a shared library
- add your command and query clients to your services and configure them to use the HTTP transport
- configure your clients as required with middlewares and HTTP settings

As the next step we recommend that you explore how to [create a clean architecture with commands and queries](../clean-architecture#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.cqs.advanced.calling-http]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
