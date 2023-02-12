# Conqueror recipe (CQS Advanced): exposing commands and queries via HTTP

This recipe shows how simple it is to expose your commands and queries to others via HTTP with **Conqueror.CQS**.

This is an advanced recipe which builds upon the concepts introduced in the [recipes about CQS basics](../../../../../..#cqs-basics). If you have not yet read those recipes, we recommend you take a look at them before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/exposing-via-http) and open the solution file in your IDE (note that you need to have [.NET 6 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/advanced/exposing-via-http).

The application, for which we will expose commands and queries, is managing a set of named counters. In code, the API of our application is represented with the following types:

```cs
public sealed record IncrementCounterByCommand(string CounterName, [Range(1, int.MaxValue)] int IncrementBy);

public sealed record IncrementCounterByCommandResponse(int NewCounterValue);

public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);
```

The standard for building HTTP APIs with .NET is ASP.NET Core. Our application is already [set up as an ASP.NET Core app](Conqueror.Recipes.CQS.Advanced.ExposingViaHttp/Program.cs), and can be launched as is, but it doesn't have any HTTP endpoints just yet.

> If your application uses a `Startup.cs` class, make sure that `FinalizeConquerorRegistrations` is called as the last step in `ConfigureServices`.

The first step for exposing commands and queries via HTTP is to add a new package dependency:

```sh
dotnet add package Conqueror.CQS.Transport.Http.Server.AspNetCore
```

As the name of the package implies, it contains the necesary logic for registering commands and queries with ASP.NET Core. In ASP.NET Core applications, you typically use **Controllers** to define HTTP endpoints. Manually creating controllers for commands and queries can be done (and we'll take a look at how to do that further down below), but a simpler way, which is the preferred approach with **Conqueror.CQS**, is to dynamically generate the necessary controllers. To get this working, we first need to let ASP.NET Core know about those controllers. Make the following change in [Program.cs](Conqueror.Recipes.CQS.Advanced.ExposingViaHttp/Program.cs):

```diff
- builder.Services.AddControllers();
+ builder.Services
+        .AddControllers()
+        .AddConquerorCQSHttpControllers();
```

Next, we need to specify which commands and queries we want to expose via HTTP. This is done by decorating a command or query with an attribute. For commands, we use the `HttpCommand` attribute, and for queries we use the `HttpQuery` attribute. Let`s add those attributes to the command and query in our application:

```diff
+ [HttpCommand]
  public sealed record IncrementCounterByCommand(string CounterName, [Range(1, int.MaxValue)] int IncrementBy);
```

```diff
+ [HttpQuery]
  public sealed record GetCounterValueQuery(string CounterName);
```

Those few changes are all that is required to expose the command and query via HTTP. You can launch the app and try it out:

```sh
curl http://localhost:5000/api/commands/incrementCounterBy --data '{"counterName":"test","incrementBy":2}' -H 'Content-Type: application/json'
# prints {"newCounterValue":2}

curl http://localhost:5000/api/queries/getCounterValue?counterName=test
# prints {"counterValue":2}
```

Because our application also has [Swagger](https://swagger.io) enabled via the [Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore) package, the command and query are visible in the [Swagger UI](https://swagger.io/tools/swagger-ui/) at [http://localhost:5000/swagger](http://localhost:5000/swagger).

**Conqueror.CQS** achieves this by using reflection and dynamic code generation to create an API controller for each command and query that is decorated with the corresponding attribute. The benefit of this approach is that from the point of view of ASP.NET Core, the dynamic controllers are indistinguishable from other controllers. This means that all ASP.NET Core features like routing, data validation, swagger integration, etc. work out of the box with those controllers. You can try the validation by calling the `incrementCounterBy` endpoint with an invalid command:

```sh
curl http://localhost:5000/api/commands/incrementCounterBy --data '{"counterName":"test","incrementBy":-1}' -H 'Content-Type: application/json'
# prints something like this: {"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1","title":"One or more validation errors occurred.","status":400,"traceId":"00-8b8dbffffda7acf68fdb8ba012a6ca9a-edd0de68b1444137-00","errors":{"IncrementBy":["The field IncrementBy must be between 1 and 2147483647."]}}
```

HTTP commands and queries run through two pipelines. First, they run through the [ASP.NET Core middleware pipeline](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/) and afterwards they run through the **Conqueror.CQS** middleware pipeline (if the handler has a pipeline configured). We recommend that you only address HTTP-specific concerns in the ASP.NET Core pipeline and handle all other concerns in the **Conqueror** pipeline, in order to keep your application logic transport-agnostic and reusable.

> There are some tricky cross-cutting concerns which require interaction between the two pipelines. One such concern is authentication. There is a [dedicated recipe](../../cross-cutting-concerns/auth#readme) which discusses how to combine an ASP.NET Core pipeline and a **Conqueror** pipeline to authenticate commands and queries. Another aspect to keep in mind is that certain built-in functionality from ASP.NET Core may be redundant with **Conqueror.CQS** middlewares, for example validation. As you saw in our example above, ASP.NET Core automatically performed data annotation validation for our command. If you have a middleware for such a concern in your **Conqueror** pipeline, we recommend that you keep it there to ensure that the cross-cutting concern is still addressed if your commands or queries are executed directly instead of via HTTP. The only downside to this may be slight performance impact due to the redundant code execution, but depending on the nature of the middleware this is typically negligible. There are even ways to configure the pipeline so that a middleware is skipped if it is executed as part of an HTTP request, but that goes beyond the scope of this recipe and is left as an exercise for the reader.

**Conqueror.CQS** sets a few defaults for the HTTP endpoints it creates. Let's take a look at what those defaults are and how they can be modified.

First, let's talk about [HTTP response status codes](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status). For successful queries, **Conqueror.CQS** will always return status code `200`. For commands without response, it will return `204`, otherwise it returns `200` as well. Status codes for unsuccessful commands and queries are not set by **Conqueror.CQS** and depend on the setup of your application (e.g. by default ASP.NET Core will return status code `400` for validation failures, status code `500` for unhandled exceptions etc.).

One of the goals of **Conqueror.CQS** is to be as transport-agnostic as possible. This means that certain information which is often encoded in transport-specific metadata should instead be embedded into the response objects directly. One example of this is returning the HTTP status code `404` when a query fails to find an entity it is looking for. With **Conqueror.CQS** the recommended approach is to instead signal success or failure through the response, for example by adding a `bool Found` property. This allows the query to be used directly or via various transports without needing to account for HTTP-specifics. One downside to this approach is that some HTTP clients or monitoring tools may depend on the convention of returning `404` when an entity is not found. If you want or need to have full control over status codes you can achieve that by creating custom controllers as shown further down below. For more control over failure error codes you can also use the [problem details](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling#problem-details) mechanism (for example, for the `404` case above you could create an `EntityNotFoundException` which is mapped to a 404 response in an error handling ASP.NET Core middleware).

Next, let's talk about [HTTP methods](https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods). By default, commands are exposed as an endpoint using the `POST` verb (taking the command's payload as the request body) and queries are exposed as an endpoint using the `GET` verb (taking the query's payload as query parameters). This matches most people's intution about what these verbs are used for. However, you may have queries for which it is not suitable to expose them via `GET` since their payload is too complex to fit into a query string. For those cases, you can explicitly expose the query via `POST`:

```cs
[HttpQuery(UsePost = true)]
public sealed record GetCounterValueQuery(string CounterName);
```

We recommend that you always expose commands via `POST` and queries via `GET` where possible, and via `POST` otherwise. There is typically enough meaning in the name of commands and queries that using other verbs like `DELETE` does not add any extra value. However, if you would like to use other verbs, this can be done by creating custom controllers as shown further down below.

Next, let's talk about HTTP paths. By default, the path for commands is determined by stripping the `Command` suffix from the command class's name, and prefixing that name with `/api/commands/`. Similarly, for queries the `Query` suffix is stripped from the query class's name, and then it is prefixed with `/api/queries/`. There are a few ways how the path can be customized.

The first approach is by using versioning. Versioning a command or query allows for changes to them to be explicitly visible to consumers of the HTTP API. Discussing all the ins and outs of HTTP API versioning goes beyond the scope of this recipe, but there is plenty of information about this topic to be found on the internet. Here, we are simply going to take a look at how versioning can be done with **Conqueror.CQS**. And it is in fact quite simple. Both the `HttpCommand` and `HttpQuery` attributes have a `Version` property of type `string`, which can be set to any value according to your versioning strategy. A common approach is to use `v1`, `v2`, etc. Let's mark our command as version 2:

```cs
[HttpCommand(Version = "v2")]
public sealed record IncrementCounterByCommand(string CounterName, [Range(1, int.MaxValue)] int IncrementBy);
```

The version string will be placed as a path segment after `/api`, i.e. for our command the path becomes `/api/v2/commands/incrementCounterBy`. Placing the version at that spot in the path is useful for routing requests between multiple versions of the command (which is another advanced topic which goes beyond the scope of this recipe).

> To ensure consistency across the version of all your commands and queries you can create a static class `ApiVersion` and add constants like `V1`, `V2`, etc. (or `Default` to make all endpoints use the same version). Then the command could be decorated with `[HttpCommand(Version = ApiVersion.V2)]`.

Another way to customize the path is to explicity set it per command or query. Both the `HttpCommand` and `HttpQuery` attributes have a `Path` property which allows overriding the complete path (if the attribute's `Version` property is also specified, it is ignored for the path). Let's do that for our query:

```cs
[HttpQuery(Path = "/api/getCounterValue")]
public sealed record GetCounterValueQuery(string CounterName);
```

You can launch the app and check that the path was set correctly in the [Swagger UI](http://localhost:5000/swagger).

The last, and most flexible, approach for customizing paths, is to provide custom path conventions. You can define a custom convention for [commands](../../../../src/Conqueror.CQS.Transport.Http.Abstractions/IHttpCommandPathConvention.cs) and for [queries](../../../../src/Conqueror.CQS.Transport.Http.Abstractions/IHttpQueryPathConvention.cs). Internally, **Conqueror.CQS** is using a default path convention (for [commands](../../../../src/Conqueror.CQS.Transport.Http.Abstractions/DefaultHttpCommandPathConvention.cs) and [queries](../../../../src/Conqueror.CQS.Transport.Http.Abstractions/DefaultHttpQueryPathConvention.cs)) which can be overridden when adding the controllers. Let's try this out by creating a custom command path convention which omits the `/commands` segment from the path. Create a new file `CustomHttpCommandPathConvention.cs`:

```cs
using System.Text.RegularExpressions;

namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

internal sealed class CustomHttpCommandPathConvention : IHttpCommandPathConvention
{
    public string GetCommandPath(Type commandType, HttpCommandAttribute attribute)
    {
        if (attribute.Path != null)
        {
            return attribute.Path;
        }

        var versionPart = attribute.Version is null ? string.Empty : $"{attribute.Version}/";
        var namePart = Regex.Replace(commandType.Name, "Command$", string.Empty);
        return $"/api/{versionPart}{namePart}";
    }
}
```

> Note that a convention is allowed to return `null`. In that case, **Conqueror.CQS** will fall back to the default convention. This allows setting conventions which only affect certain commands and queries in your application.

We also need to let **Conqueror.CQS** know about our convention. Make the following change in `Program.cs`:

```diff
builder.Services
       .AddControllers()
-      .AddConquerorCQSHttpControllers();
+      .AddConquerorCQSHttpControllers(o => o.CommandPathConvention = new CustomHttpCommandPathConvention());
```

If you now launch the application, you will see that the command has the path `/api/v2/incrementCounterBy`.

The last thing you can customize via the attributes are certain metadata values, which are used by tools like [Swashbuckle](https://www.nuget.org/packages/Swashbuckle.AspNetCore) to generate API documentation. The two metadata properties you can specify are `OperationId` and `ApiGroupName`. The default value for `OperationId` is the full name of the command or query type (e.g. `Conqueror.Recipes.CQS.Advanced.ExposingViaHttp.IncrementCounterByCommand`). The `ApiGroupName` is empty by default. Setting the `ApiGroupName` also allows grouping commands and queries in Swagger UI (where by default all commands are grouped together and all queries are grouped together). Note that if you set a custom `ApiGroupName`, you also need to specify a custom `DocInclusionPredicate` on the swagger generation options in `Program.cs` to ensure the endpoints still show up in the default document:

```diff
  builder.Services
         .AddEndpointsApiExplorer()
-        .AddSwaggerGen();
+        .AddSwaggerGen(c => c.DocInclusionPredicate((_, _) => true));
```

The defaults and customization options shown above are designed to suit the most common use cases and allow exposing commands and queries via HTTP with minimal boilerplate code. However, if the customization options are not sufficient for you, you can create your own controllers. This provides you with all the control you need. However, you need to execute the command or query with a special helper class, which takes care of some internal aspects of **Conqueror.CQS**. Let's take a look at how this works by creating a custom controller for our command. We want the command to return status code `201` instead of `200` on success (this does not fit the intention of `201`, but serves as a good demonstration for how to build custom controllers). Create a new file called `IncrementCounterByCommandController.cs`:

```cs
using Conqueror.CQS.Transport.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

[ApiController]
public sealed class IncrementCounterByCommandController : ControllerBase
{
    [HttpPost("/api/custom/incrementCounterBy")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(IncrementCounterByCommandResponse))]
    public async Task<IActionResult> ExecuteCommand(IncrementCounterByCommand command, CancellationToken cancellationToken)
    {
        var response = await HttpCommandExecutor.ExecuteCommand<IncrementCounterByCommand, IncrementCounterByCommandResponse>(HttpContext, command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
```

As you can see, this is a completely normal controller, but it uses the helper class `HttpCommandExecutor` to execute the command (for queries you would use the `HttpQueryExecutor`).

> You probably also want to remove the `HttpCommand` attribute from the command, since otherwise it will be exposed via **Conqueror**'s dynamic controller as well as your own.

And that concludes this recipe for exposing your commands and queries via HTTP with **Conqueror.CQS**. In summary, you need to do the following:

- add a reference to the [Conqueror.CQS.Transport.Http.Server.AspNetCore](https://www.nuget.org/packages/Conqueror.CQS.Transport.Http.Server.AspNetCore/) package
- add command and query controllers via `AddConquerorCQSHttpControllers`
- decorate your commands with `[HttpCommand]` and your queries with `[HttpQuery]`
- customize the behavior as required (we recommend you stick to the defaults as much as possible to keep your app simple)

As the next step we recommend that you explore [how to test HTTP commands and queries](../testing-http#readme) or how to [call your HTTP commands and queries](../calling-http#readme) from another application.

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.cqs.advanced.exposing-via-http]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
