# Conqueror recipe (Messaging): exposing messages via HTTP

This recipe shows how simple it is to expose your messages to others via HTTP with **Conqueror**.

This is an advanced recipe which builds upon the concepts introduced in the [recipes about messaging basics](../../../../../..#messaging-basics). If you have not yet read those recipes, we recommend you take a look at them before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/transports/http/recipes/messaging/exposing-via-http) and open the solution file in your IDE (note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/transports/http/recipes/messaging/exposing-via-http).

The application, for which we will expose messages, is managing a set of named counters. In code, the API of our application is represented with the following types:

```cs
[Message<IncrementCounterResponse>]
public partial record IncrementCounter(string CounterName);

public record IncrementCounterResponse(int NewCounterValue);

[Message<GetCounterValueResponse>]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);
```

Feel free to take a look at the full code for [incrementing a counter](.completed/Conqueror.Recipes.Messaging.ExposingViaHttp/IncrementCounter.cs) and [getting a counter's value](.completed/Conqueror.Recipes.Messaging.ExposingViaHttp/GetCounterValue.cs). The counters are stored in an [in-memory repository](.completed/Conqueror.Recipes.Messaging.ExposingViaHttp/CountersRepository.cs).

The standard for building HTTP APIs with .NET is ASP.NET Core. Our application is already [set up as an ASP.NET Core app](Conqueror.Recipes.Messaging.ExposingViaHttp/Program.cs), and can be launched as is, but it doesn't have any HTTP endpoints just yet.

The first step for exposing messages via HTTP is to add a new package dependency:

```sh
dotnet add Conqueror.Recipes.Messaging.ExposingViaHttp package Conqueror.Transport.Http.Server.AspNetCore
```

As the name of the package implies, it contains the necessary logic for exposing messages with ASP.NET Core. **Conqueror** exposes message handlers as [minimal API endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview). To get this working, we first need to add the services which the transport requires. Make the following change in [Program.cs](Conqueror.Recipes.Messaging.ExposingViaHttp/Program.cs) (`AddConquerorHttpServerAspNetCore` registers the endpoints API explorer among other services, so we can drop the explicit call):

```diff
  builder.Services
-     .AddEndpointsApiExplorer()
+     .AddConquerorHttpServerAspNetCore()
      .AddSwaggerGen();
```

We also need to map the endpoints for our messages. This is done with a single call, optimally placed just before running the app:

```diff
  app.UseSwagger();
  app.UseSwaggerUI();

+ app.MapMessageEndpoints();

  await app.RunAsync();
```

Next, we need to specify which messages we want to expose via HTTP. This is done by replacing the `Message` attribute with the `HttpMessage` attribute. By default, a message is exposed with the `POST` method; for messages which read data, it usually makes more sense to use `GET`, which can be set explicitly via the attribute's `HttpMethod` property. We also set an explicit version for our messages, which becomes part of the endpoint's path (more on paths further down below). Let's adjust the attributes on the messages in our application:

```diff
- [Message<IncrementCounterResponse>]
+ [HttpMessage<IncrementCounterResponse>(Version = "v1")]
  public partial record IncrementCounter(string CounterName);
```

```diff
- [Message<GetCounterValueResponse>]
+ [HttpMessage<GetCounterValueResponse>(HttpMethod = "GET", Version = "v1")]
  public partial record GetCounterValue(string CounterName);
```

Those few changes are all that is required to expose the messages via HTTP. You can launch the app and try it out:

```sh
curl http://localhost:5000/api/v1/incrementCounter --data '{"counterName":"test"}' -H 'Content-Type: application/json'
# prints {"newCounterValue":1}

curl http://localhost:5000/api/v1/getCounterValue?counterName=test
# prints {"counterExists":true,"counterValue":1}
```

Because our application also has [Swagger](https://swagger.io) enabled via the [Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore) package, the messages are visible in the [Swagger UI](https://swagger.io/tools/swagger-ui/) at [http://localhost:5000/swagger](http://localhost:5000/swagger).

**Conqueror** achieves this without any reflection or dynamic code generation. A source generator creates metadata for each message type which is decorated with the `HttpMessage` attribute, and `MapMessageEndpoints` uses this metadata to create a minimal API endpoint for each message handler (this is also what makes the HTTP transport work with native AOT). From the point of view of ASP.NET Core, these endpoints are indistinguishable from hand-written minimal API endpoints. This means that ASP.NET Core features like routing, authentication, and OpenAPI integration work out of the box with those endpoints.

> In contrast to ASP.NET Core controllers, minimal API endpoints do not automatically perform data annotation validation. With **Conqueror**, validation is a cross-cutting concern which is best addressed with a middleware in the message pipeline, which ensures that the validation also takes place when your messages are executed in-process or via a different transport. Take a look at the recipe for [solving cross-cutting concerns](../../../../../core/recipes/messaging/solving-cross-cutting-concerns#readme) to see how that works.

HTTP messages run through two pipelines. First, they run through the [ASP.NET Core middleware pipeline](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/) and afterwards they run through the **Conqueror** middleware pipeline (if the handler has a pipeline configured). We recommend that you only address HTTP-specific concerns in the ASP.NET Core pipeline and handle all other concerns in the **Conqueror** pipeline, in order to keep your application logic transport-agnostic and reusable.

**Conqueror** sets a few defaults for the HTTP endpoints it creates. Let's take a look at what those defaults are and how they can be modified.

First, let's talk about [HTTP response status codes](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status). For successful messages, **Conqueror** will return status code `200` (or `204` for messages without a response). This can be changed with the attribute's `SuccessStatusCode` property:

```cs
[HttpMessage<IncrementCounterResponse>(Version = "v1", SuccessStatusCode = 201)]
public partial record IncrementCounter(string CounterName);
```

Status codes for unsuccessful messages are not set by **Conqueror** and depend on the setup of your application (e.g. by default ASP.NET Core will return status code `500` for unhandled exceptions).

One of the goals of **Conqueror** is to be as transport-agnostic as possible. This means that certain information which is often encoded in transport-specific metadata should instead be embedded into the response objects directly. One example of this is returning the HTTP status code `404` when a message fails to find an entity it is looking for. With **Conqueror** the recommended approach is to instead signal success or failure through the response, for example by adding a `bool Found` property (which is exactly what the `CounterExists` property on our `GetCounterValueResponse` does). This allows the message to be used directly or via various transports without needing to account for HTTP-specifics. One downside to this approach is that some HTTP clients or monitoring tools may depend on the convention of returning `404` when an entity is not found. If you want or need to have full control over status codes you can achieve that by creating custom endpoints as shown further down below. For more control over failure error codes you can also use the [problem details](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling#problem-details) mechanism (for example, for the `404` case above you could create an `EntityNotFoundException` which is mapped to a 404 response in an error handling ASP.NET Core middleware).

Next, let's talk about [HTTP methods](https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods). As mentioned above, by default a message is exposed as an endpoint using the `POST` method, taking the message's payload as the request body. When setting the `HttpMethod` property to `GET`, the message's payload is instead taken from the query string parameters. We recommend that you expose messages which write data via `POST`, and messages which read data via `GET` where possible, and via `POST` otherwise (e.g. if their payload is too complex to fit into a query string). There is typically enough meaning in the name of a message that using other methods like `DELETE` does not add any extra value. However, if you would like to use other methods, you can set them via the `HttpMethod` property as well, or create custom endpoints as shown further down below.

Next, let's talk about HTTP paths. By default, the path for a message is determined by stripping the `Message` suffix (if any) from the message type's name, lower-casing the first character, and prefixing it with `api/`. There are a few ways how the path can be customized.

The first approach is by using versioning. Versioning a message allows for changes to it to be explicitly visible to consumers of the HTTP API. Discussing all the ins and outs of HTTP API versioning goes beyond the scope of this recipe, but there is plenty of information about this topic to be found on the internet. Here, we are simply going to take a look at how versioning can be done with **Conqueror**. And it is in fact quite simple. The `HttpMessage` attribute has a `Version` property of type `string`, which can be set to any value according to your versioning strategy. A common approach is to use `v1`, `v2`, etc., and we already marked our messages as version 1 when we added the attributes above. The version string is placed as a path segment after the `api` prefix, i.e. for our messages the paths are `/api/v1/incrementCounter` and `/api/v1/getCounterValue`. Placing the version at that spot in the path is useful for routing requests between multiple versions of a message (which is another advanced topic which goes beyond the scope of this recipe).

> To ensure consistency across the versions of all your messages you can create a static class `ApiVersion` and add constants like `V1`, `V2`, etc. (or `Default` to make all endpoints use the same version). Then the message could be decorated with `[HttpMessage<IncrementCounterResponse>(Version = ApiVersion.V1)]`.

Another way to customize the path is to explicitly set it per message. The `HttpMessage` attribute has a `Path` property which overrides the name-based path segment. Let's do that for our `GetCounterValue` message:

```diff
- [HttpMessage<GetCounterValueResponse>(HttpMethod = "GET", Version = "v1")]
+ [HttpMessage<GetCounterValueResponse>(HttpMethod = "GET", Version = "v1", Path = "counterValue")]
  public partial record GetCounterValue(string CounterName);
```

You can launch the app and check that the message is now reachable at the new path:

```sh
curl http://localhost:5000/api/v1/counterValue?counterName=test
# prints {"counterExists":false,"counterValue":null}
```

For even more control, the `PathPrefix` property allows changing the `api` prefix, and the `FullPath` property allows overriding the complete path (if `FullPath` is set, the `PathPrefix`, `Version`, and `Path` properties are ignored).

The last thing you can customize via the attribute are certain metadata values, which are used by tools like [Swashbuckle](https://www.nuget.org/packages/Swashbuckle.AspNetCore) to generate API documentation. The two metadata properties you can specify are `Name` and `ApiGroupName`. The default value for `Name` is the name of the message type (e.g. `IncrementCounter`). The `ApiGroupName` is empty by default. Setting the `ApiGroupName` allows grouping messages in Swagger UI. Let's do that for our `IncrementCounter` message:

```diff
- [HttpMessage<IncrementCounterResponse>(Version = "v1")]
+ [HttpMessage<IncrementCounterResponse>(Version = "v1", ApiGroupName = "Counters")]
  public partial record IncrementCounter(string CounterName);
```

Note that if you set a custom `ApiGroupName`, you also need to specify a custom `DocInclusionPredicate` on the swagger generation options in `Program.cs` to ensure the endpoints still show up in the default document:

```diff
  builder.Services
      .AddConquerorHttpServerAspNetCore()
-     .AddSwaggerGen();
+     .AddSwaggerGen(c => c.DocInclusionPredicate((_, _) => true));
```

The defaults and customization options shown above are designed to suit the most common use cases and allow exposing messages via HTTP with minimal boilerplate code. However, if the customization options are not sufficient for you, you can create your own endpoints, in which you call the message handler through `IMessageSenders`, just like you would anywhere else in your application. This provides you with all the control you need. Let's take a look at how this works by creating a custom endpoint for our `IncrementCounter` message. We want the message to return status code `201` instead of `200` on success (this does not fit the intention of `201`, but serves as a good demonstration for how to build custom endpoints). Add the following endpoint in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.ExposingViaHttp/Program.cs)):

```cs
app.MapPost("/api/custom/incrementCounter",
    async (IncrementCounter message, IMessageSenders senders, CancellationToken cancellationToken) =>
    {
        var response = await senders.For(IncrementCounter.T).Handle(message, cancellationToken);
        return Results.Json(response, statusCode: StatusCodes.Status201Created);
    });
```

As you can see, this is a completely normal minimal API endpoint that you can structure in any way you like (and if you prefer controllers over minimal APIs, you can call the message handler from a controller in just the same way).

Let's also create a custom endpoint for the `GetCounterValue` message. We use the `[AsParameters]` attribute to populate the message from the HTTP query parameters:

```cs
app.MapGet("/api/custom/getCounterValue",
    ([AsParameters] GetCounterValue message, IMessageSenders senders, CancellationToken cancellationToken) =>
        senders.For(GetCounterValue.T).Handle(message, cancellationToken));
```

For messages without a payload, you can simply omit the corresponding parameter and pass an empty message to the sender, for example like this:

```cs
app.MapGet("/api/custom/myMessageWithoutPayload",
    (IMessageSenders senders, CancellationToken cancellationToken) =>
        senders.For(MyMessageWithoutPayload.T).Handle(new(), cancellationToken));
```

> You probably also want to change the `HttpMessage` attribute back to the plain `Message` attribute for any message that you create custom endpoints for, since otherwise it will be exposed via the endpoints created by `MapMessageEndpoints` as well as your own.

And that concludes this recipe for exposing your messages via HTTP with **Conqueror**. In summary, you need to do the following:

- add a reference to the [Conqueror.Transport.Http.Server.AspNetCore](https://www.nuget.org/packages/Conqueror.Transport.Http.Server.AspNetCore/) package
- add the transport's services via `AddConquerorHttpServerAspNetCore` and map the endpoints via `MapMessageEndpoints`
- decorate your messages with `[HttpMessage<TResponse>]`
- customize the behavior as required (we recommend you stick to the defaults as much as possible to keep your app simple)

As the next step we recommend that you explore [how to test HTTP messages](../testing-http#readme) or how to [call your HTTP messages](../calling-http#readme) from another application.

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.exposing-via-http]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
