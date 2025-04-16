using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Conqueror.Transport.Http.Server.AspNetCore.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using static Conqueror.ConquerorTransportHttpConstants;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Conqueror.Transport.Http.Tests.Messaging;

[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Members are used by ASP.NET Core via reflection")]
public static partial class HttpTestMessages
{
    public delegate Task FnToCallFromHandler(IServiceProvider serviceProvider);

    public enum MessageTestCaseRegistrationMethod
    {
        Controllers,
        ExplicitController,
        CustomController,
        Endpoints,
        ExplicitEndpoint,
        CustomEndpoint,
    }

    public static void RegisterMessageType<TMessage, TResponse, THandler>(this IServiceCollection services, MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where THandler : class, IGeneratedMessageHandler
    {
        _ = services.AddSingleton<TestObservations>()
                    .AddTransient(typeof(TestMessageMiddleware<,>));

        _ = services.AddMessageHandler<THandler>();

        if (typeof(TMessage) == typeof(TestMessageWithCustomSerializedPayloadType))
        {
            _ = services.AddTransient<JsonSerializerOptions>(p => p.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions)
                        .PostConfigure<JsonOptions>(options =>
                        {
                            options.SerializerOptions.Converters
                                   .Add(new TestMessageWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory());
                        })
                        .PostConfigure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
                        {
                            options.JsonSerializerOptions.Converters
                                   .Add(new TestMessageWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory());
                        });
        }

        if (testCase.RegistrationMethod
            is MessageTestCaseRegistrationMethod.Endpoints
            or MessageTestCaseRegistrationMethod.ExplicitEndpoint
            or MessageTestCaseRegistrationMethod.CustomEndpoint)
        {
            _ = services.AddRouting()
                        .AddEndpointsApiExplorer()
                        .AddMessageEndpoints();
            return;
        }

        var mvcBuilder = services.AddControllers();

        if (testCase.RegistrationMethod == MessageTestCaseRegistrationMethod.Controllers)
        {
            _ = mvcBuilder.AddMessageControllers();
            return;
        }

        if (testCase.RegistrationMethod == MessageTestCaseRegistrationMethod.CustomController)
        {
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
            applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());
            _ = services.AddSingleton(applicationPartManager);
            return;
        }

        _ = mvcBuilder.AddMessageController<TMessage>();
    }

    public static void MapMessageEndpoints<TMessage, TResponse>(this IApplicationBuilder app, MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        _ = app.UseConqueror();
        _ = app.UseRouting();

        if (testCase.RegistrationMethod
            is MessageTestCaseRegistrationMethod.Controllers
            or MessageTestCaseRegistrationMethod.ExplicitController
            or MessageTestCaseRegistrationMethod.CustomController)
        {
            _ = app.UseEndpoints(b => b.MapControllers());

            return;
        }

        _ = app.UseEndpoints(endpoints =>
        {
            endpoints.MapMethods("/debug/{param:int}", ["GET"], (int param, HttpContext _) => TypedResults.Ok(param))
                     .Finally(e =>
                     {
                         // to allow stepping in with debugger
                         _ = e;
                     });

            if (testCase.RegistrationMethod is MessageTestCaseRegistrationMethod.Endpoints)
            {
                _ = endpoints.MapMessageEndpoints();
                return;
            }

            if (testCase.RegistrationMethod is MessageTestCaseRegistrationMethod.CustomEndpoint)
            {
                _ = endpoints.MapPost("/custom/api/test",
                                      async (TestMessage message, HttpContext ctx)
                                          => TypedResults.Ok(await ctx.GetMessageClient(TestMessage.T).Handle(message, ctx.RequestAborted)))
                             .WithName(nameof(TestMessage));

                _ = endpoints.MapPost("/custom/api/testMessageWithoutPayload",
                                      (Delegate)(async (HttpContext ctx)
                                          => TypedResults.Ok(await ctx.GetMessageClient(TestMessageWithoutPayload.T).Handle(new(), ctx.RequestAborted))))
                             .WithName(nameof(TestMessageWithoutPayload));

                _ = endpoints.MapPost("/custom/api/testMessageWithoutResponse",
                                      async (TestMessageWithoutResponse message, HttpContext ctx) =>
                                      {
                                          await ctx.GetMessageClient<TestMessageWithoutResponse>().Handle(message, ctx.RequestAborted);
                                          return TypedResults.Ok();
                                      })
                             .WithName(nameof(TestMessageWithoutResponse));

                _ = endpoints.MapPost("/custom/api/testMessageWithoutResponseWithoutPayload",
                                      async (HttpContext ctx) =>
                                      {
                                          await ctx.GetMessageClient<TestMessageWithoutResponseWithoutPayload>().Handle(new(), ctx.RequestAborted);
                                          return TypedResults.StatusCode(200);
                                      })
                             .WithName(nameof(TestMessageWithoutResponseWithoutPayload));

                _ = endpoints.MapGet("/custom/api/testMessageWithGet",
                                     async (int payload, string param, HttpContext ctx)
                                         => TypedResults.Ok(await ctx.GetMessageClient<TestMessageWithGet, TestMessageResponse>() // testing overload without inference
                                                                     .Handle(new() { Payload = payload, Param = param }, ctx.RequestAborted)))
                             .WithName(nameof(TestMessageWithGet));

                _ = endpoints.MapGet("/custom/api/testMessageWithGetWithoutPayload",
                                     (Delegate)(async (HttpContext ctx)
                                         => TypedResults.Ok(await ctx.GetMessageClient(TestMessageWithGetWithoutPayload.T).Handle(new(), ctx.RequestAborted))))
                             .WithName(nameof(TestMessageWithGetWithoutPayload));

                _ = endpoints.MapPost("/custom/api/testMessageWithMiddleware",
                                      async (TestMessageWithMiddleware message, HttpContext ctx)
                                          => TypedResults.Ok(await ctx.GetMessageClient(TestMessageWithMiddleware.T).Handle(message, ctx.RequestAborted)))
                             .WithName(nameof(TestMessageWithMiddleware));

                return;
            }

            _ = endpoints.MapMessageEndpoint<TMessage>();
        });
    }

    public static IEnumerable<TestCaseData> GenerateTestCaseData()
    {
        return GenerateTestCases().Select(c => new TestCaseData(c)
        {
            TypeArgs = [c.MessageType, c.ResponseType ?? typeof(UnitMessageResponse), c.HandlerType],
        });
    }

    private static IEnumerable<MessageTestCase> GenerateTestCases()
    {
        foreach (var registrationMethod in new[]
                 {
                     MessageTestCaseRegistrationMethod.Controllers,
                     MessageTestCaseRegistrationMethod.ExplicitController,
                     MessageTestCaseRegistrationMethod.Endpoints,
                     MessageTestCaseRegistrationMethod.ExplicitEndpoint,
                 })
        {
            yield return new()
            {
                MessageType = typeof(TestMessage),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/test",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessage { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithoutResponse),
                ResponseType = null,
                HandlerType = typeof(TestMessageWithoutResponseHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithoutResponse",
                SuccessStatusCode = 204,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = string.Empty,
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = null,
                Message = new TestMessageWithoutResponse { Payload = 10 },
                Response = null,
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithoutPayload),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithoutPayloadHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithoutPayload",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 0,
                QueryString = null,
                Payload = null,
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = null,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithoutPayload(),
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithoutResponseWithoutPayload),
                ResponseType = null,
                HandlerType = typeof(TestMessageWithoutResponseWithoutPayloadHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithoutResponseWithoutPayload",
                SuccessStatusCode = 204,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 0,
                QueryString = null,
                Payload = null,
                ResponsePayload = string.Empty,
                MessageContentType = null,
                ResponseContentType = null,
                Message = new TestMessageWithoutResponseWithoutPayload(),
                Response = null,
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithMethod),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithMethodHandler),
                HttpMethod = MethodDelete,
                FullPath = "/api/testMessageWithMethod",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithMethod { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithPathPrefix),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithPathPrefixHandler),
                HttpMethod = MethodPost,
                FullPath = "/custom/prefix/testMessageWithPathPrefix",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithPathPrefix { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithVersion),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithVersionHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/v2/testMessageWithVersion",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithVersion { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithPath),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithPathHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/custom/path",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithPath { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithPathPrefixAndPathAndVersion),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithPathPrefixAndPathAndVersionHandler),
                HttpMethod = MethodPost,
                FullPath = "/custom/prefix/v3/custom/path",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithPathPrefixAndPathAndVersion { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithFullPath),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithFullPathHandler),
                HttpMethod = MethodPost,
                FullPath = "/custom/full/path/for/message",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithFullPath { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithFullPathAndVersion),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithFullPathAndVersionHandler),
                HttpMethod = MethodPost,
                FullPath = "/custom/full/path/for/message",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithFullPathAndVersion { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithSuccessStatusCode),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithSuccessStatusCodeHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithSuccessStatusCode",
                SuccessStatusCode = 201,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithSuccessStatusCode { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithName),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithNameHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithName",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = "custom-message-name",
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithName { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithApiGroupName),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithApiGroupNameHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithApiGroupName",
                SuccessStatusCode = 200,
                ApiGroupName = "Custom Message Group",
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithApiGroupName { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithGet),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithGetHandler),
                HttpMethod = MethodGet,
                FullPath = "/api/testMessageWithGet",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 2,
                QueryString = "?payload=10&param=test",
                Payload = null,
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = null,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithGet { Payload = 10, Param = "test" },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithGetWithoutPayload),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithGetWithoutPayloadHandler),
                HttpMethod = MethodGet,
                FullPath = "/api/testMessageWithGetWithoutPayload",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 0,
                QueryString = null,
                Payload = null,
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = null,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithGetWithoutPayload(),
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            // complex get queries are not supported in minimal API
            if (registrationMethod
                is not MessageTestCaseRegistrationMethod.Endpoints
                and not MessageTestCaseRegistrationMethod.ExplicitEndpoint)
            {
                yield return new()
                {
                    MessageType = typeof(TestMessageWithComplexGetPayload),
                    ResponseType = typeof(TestMessageResponse),
                    HandlerType = typeof(TestMessageWithComplexGetPayloadHandler),
                    HttpMethod = MethodGet,
                    FullPath = "/api/testMessageWithComplexGetPayload",
                    SuccessStatusCode = 200,
                    ApiGroupName = null,
                    Name = null,
                    ParameterCount = 5,
                    QueryString = "?payload=10&nestedPayload.payload=11&nestedPayload.payload2=12&nestedList=13&nestedList=14&nestedArray=15&nestedArray=16",
                    Payload = null,
                    ResponsePayload = "{\"payload\":91}",
                    MessageContentType = null,
                    ResponseContentType = MediaTypeNames.Application.Json,
                    Message = new TestMessageWithComplexGetPayload
                    {
                        Payload = 10,
                        NestedPayload = new() { Payload = 11, Payload2 = 12 },
                        NestedList = [13, 14],
                        NestedArray = [15, 16],
                    },
                    Response = new TestMessageResponse { Payload = 91 },
                    RegistrationMethod = registrationMethod,
                };
            }

            yield return new()
            {
                MessageType = typeof(TestMessageWithCustomSerializedPayloadType),
                ResponseType = typeof(TestMessageWithCustomSerializedPayloadTypeResponse),
                HandlerType = typeof(TestMessageWithCustomSerializedPayloadTypeHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithCustomSerializedPayloadType",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithCustomSerializedPayloadType { Payload = new(10) },
                Response = new TestMessageWithCustomSerializedPayloadTypeResponse { Payload = new(11) },
                RegistrationMethod = registrationMethod,
            };

            // custom serializer and json type info is only supported in minimal API
            if (registrationMethod
                is MessageTestCaseRegistrationMethod.Endpoints
                or MessageTestCaseRegistrationMethod.ExplicitEndpoint)
            {
                yield return new()
                {
                    MessageType = typeof(TestMessageWithCustomSerializer),
                    ResponseType = typeof(TestMessageWithCustomSerializerResponse),
                    HandlerType = typeof(TestMessageWithCustomSerializerHandler),
                    HttpMethod = MethodPost,
                    FullPath = "/api/custom/path/for/serializer/12",
                    Template = "/api/custom/path/for/serializer/{pathPayload:int}",
                    SuccessStatusCode = 200,
                    ApiGroupName = null,
                    Name = null,
                    ParameterCount = 1,
                    QueryString = "?query-payload=10",
                    Payload = "{\"bodyPayload\":11}",
                    ResponsePayload = "{\"total-payload\":33}",
                    MessageContentType = "application/custom-message",
                    ResponseContentType = "application/custom-response",
                    MessageSerializer = TestMessageWithCustomSerializer.HttpMessageSerializer,
                    ResponseSerializer = TestMessageWithCustomSerializer.HttpResponseSerializer,
                    Message = new TestMessageWithCustomSerializer { QueryPayload = 10, BodyPayload = 11, PathPayload = 12 },
                    Response = new TestMessageWithCustomSerializerResponse { Payload = 33 },
                    RegistrationMethod = registrationMethod,
                };

                yield return new()
                {
                    MessageType = typeof(TestMessageWithCustomJsonTypeInfo),
                    ResponseType = typeof(TestMessageWithCustomJsonTypeInfoResponse),
                    HandlerType = typeof(TestMessageWithCustomJsonTypeInfoHandler),
                    HttpMethod = MethodPost,
                    FullPath = "/api/testMessageWithCustomJsonTypeInfo",
                    SuccessStatusCode = 200,
                    ApiGroupName = null,
                    Name = null,
                    ParameterCount = 1,
                    QueryString = null,
                    Payload = "{\"MESSAGE_PAYLOAD\":10}",
                    ResponsePayload = "{\"RESPONSE_PAYLOAD\":11}",
                    MessageContentType = MediaTypeNames.Application.Json,
                    ResponseContentType = MediaTypeNames.Application.Json,
                    JsonSerializerContext = TestMessageWithCustomJsonTypeInfo.JsonSerializerContext,
                    Message = new TestMessageWithCustomJsonTypeInfo { MessagePayload = 10 },
                    Response = new TestMessageWithCustomJsonTypeInfoResponse { ResponsePayload = 11 },
                    RegistrationMethod = registrationMethod,
                };

                yield return new()
                {
                    MessageType = typeof(TestMessageWithMiddleware),
                    ResponseType = typeof(TestMessageResponse),
                    HandlerType = typeof(TestMessageWithMiddlewareHandler),
                    HttpMethod = MethodPost,
                    FullPath = "/api/testMessageWithMiddleware",
                    SuccessStatusCode = 200,
                    ApiGroupName = null,
                    Name = null,
                    ParameterCount = 1,
                    QueryString = null,
                    Payload = "{\"payload\":10}",
                    ResponsePayload = "{\"payload\":11}",
                    MessageContentType = MediaTypeNames.Application.Json,
                    ResponseContentType = MediaTypeNames.Application.Json,
                    Message = new TestMessageWithMiddleware { Payload = 10 },
                    Response = new TestMessageResponse { Payload = 11 },
                    RegistrationMethod = registrationMethod,
                };

                yield return new()
                {
                    MessageType = typeof(TestMessageWithMiddlewareWithoutResponse),
                    ResponseType = null,
                    HandlerType = typeof(TestMessageWithMiddlewareWithoutResponseHandler),
                    HttpMethod = MethodPost,
                    FullPath = "/api/testMessageWithMiddlewareWithoutResponse",
                    SuccessStatusCode = 204,
                    ApiGroupName = null,
                    Name = null,
                    ParameterCount = 1,
                    QueryString = null,
                    Payload = "{\"payload\":10}",
                    ResponsePayload = string.Empty,
                    MessageContentType = MediaTypeNames.Application.Json,
                    ResponseContentType = null,
                    Message = new TestMessageWithMiddlewareWithoutResponse { Payload = 10 },
                    Response = null,
                    RegistrationMethod = registrationMethod,
                };
            }
        }

        foreach (var registrationMethod in new[]
                 {
                     MessageTestCaseRegistrationMethod.CustomController,
                     MessageTestCaseRegistrationMethod.CustomEndpoint,
                 })
        {
            yield return new()
            {
                MessageType = typeof(TestMessage),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageHandler),
                HttpMethod = MethodPost,
                FullPath = "/custom/api/test",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessage { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithoutResponse),
                ResponseType = null,
                HandlerType = typeof(TestMessageWithoutResponseHandler),
                HttpMethod = MethodPost,
                FullPath = "/custom/api/testMessageWithoutResponse",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = string.Empty,
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = null,
                Message = new TestMessageWithoutResponse { Payload = 10 },
                Response = null,
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithoutPayload),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithoutPayloadHandler),
                HttpMethod = MethodPost,
                FullPath = "/custom/api/testMessageWithoutPayload",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 0,
                QueryString = null,
                Payload = null,
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = null,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithoutPayload(),
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithoutResponseWithoutPayload),
                ResponseType = null,
                HandlerType = typeof(TestMessageWithoutResponseWithoutPayloadHandler),
                HttpMethod = MethodPost,
                FullPath = "/custom/api/testMessageWithoutResponseWithoutPayload",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 0,
                QueryString = null,
                Payload = null,
                ResponsePayload = string.Empty,
                MessageContentType = null,
                ResponseContentType = null,
                Message = new TestMessageWithoutResponseWithoutPayload(),
                Response = null,
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithGet),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithGetHandler),
                HttpMethod = MethodGet,
                FullPath = "/custom/api/testMessageWithGet",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 2,
                QueryString = "?payload=10&param=test",
                Payload = null,
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = null,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithGet { Payload = 10, Param = "test" },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithGetWithoutPayload),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithGetWithoutPayloadHandler),
                HttpMethod = MethodGet,
                FullPath = "/custom/api/testMessageWithGetWithoutPayload",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 0,
                QueryString = null,
                Payload = null,
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = null,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithGetWithoutPayload(),
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithMiddleware),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithMiddlewareHandler),
                HttpMethod = MethodPost,
                FullPath = "/custom/api/testMessageWithMiddleware",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithMiddleware { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };
        }

        foreach (var registrationMethod in new[]
                 {
                     MessageTestCaseRegistrationMethod.Controllers,
                     MessageTestCaseRegistrationMethod.Endpoints,
                 })
        {
            yield return new()
            {
                MessageType = typeof(TestMessageForAssemblyScanning),
                ResponseType = typeof(TestMessageForAssemblyScanningResponse),
                HandlerType = typeof(TestMessageForAssemblyScanningHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageForAssemblyScanning",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageForAssemblyScanning { Payload = 10 },
                Response = new TestMessageForAssemblyScanningResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithoutResponseForAssemblyScanning),
                ResponseType = null,
                HandlerType = typeof(TestMessageWithoutResponseForAssemblyScanningHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithoutResponseForAssemblyScanning",
                SuccessStatusCode = 204,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = string.Empty,
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = null,
                Message = new TestMessageWithoutResponseForAssemblyScanning { Payload = 10 },
                Response = null,
                RegistrationMethod = registrationMethod,
            };
        }
    }

    public sealed record MessageTestCase
    {
        public required Type MessageType { get; init; }

        public required Type? ResponseType { get; init; }

        public required Type HandlerType { get; init; }

        public required string HttpMethod { get; init; }

        public required string FullPath { get; init; }

        public string? Template { get; init; }

        public required int SuccessStatusCode { get; init; }

        public required string? ApiGroupName { get; init; }

        public required string? Name { get; init; }

        public required int ParameterCount { get; init; }

        public required MessageTestCaseRegistrationMethod RegistrationMethod { get; init; }

        public required string? QueryString { get; init; }

        public required string? Payload { get; init; }

        public required string ResponsePayload { get; init; }

        public required string? MessageContentType { get; init; }

        public required string? ResponseContentType { get; init; }

        public JsonSerializerContext? JsonSerializerContext { get; init; }

        public IHttpMessageSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>? MessageSerializer { get; init; }

        public IHttpResponseSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>? ResponseSerializer { get; init; }

        public required object Message { get; init; }

        public required object? Response { get; init; }
    }

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessage
    {
        public int Payload { get; init; }
    }

    public sealed record TestMessageResponse
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithoutPayload;

    public sealed class TestMessageWithoutPayloadHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithoutPayload.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithoutPayload message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = 11 };
        }
    }

    [HttpMessage]
    public sealed partial record TestMessageWithoutResponse
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithoutResponseHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }
        }
    }

    [HttpMessage]
    public sealed partial record TestMessageWithoutResponseWithoutPayload;

    public sealed class TestMessageWithoutResponseWithoutPayloadHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithoutResponseWithoutPayload.IHandler
    {
        public async Task Handle(TestMessageWithoutResponseWithoutPayload message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }
        }
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodDelete)]
    public sealed partial record TestMessageWithMethod
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithMethodHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithMethod.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithMethod message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(PathPrefix = "/custom/prefix")]
    public sealed partial record TestMessageWithPathPrefix
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithPathPrefixHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithPathPrefix.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithPathPrefix message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(Version = "v2")]
    public sealed partial record TestMessageWithVersion
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithVersionHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithVersion.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithVersion message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(Path = "/custom/path")]
    public sealed partial record TestMessageWithPath
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithPathHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithPath.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithPath message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(PathPrefix = "/custom/prefix", Version = "v3", Path = "/custom/path")]
    public sealed partial record TestMessageWithPathPrefixAndPathAndVersion
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithPathPrefixAndPathAndVersionHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithPathPrefixAndPathAndVersion.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithPathPrefixAndPathAndVersion message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(FullPath = "/custom/full/path/for/message")]
    public sealed partial record TestMessageWithFullPath
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithFullPathHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithFullPath.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithFullPath message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(FullPath = "/custom/full/path/for/message", Version = "v2")]
    public sealed partial record TestMessageWithFullPathAndVersion
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithFullPathAndVersionHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithFullPathAndVersion.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithFullPathAndVersion message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(SuccessStatusCode = 201)]
    public sealed partial record TestMessageWithSuccessStatusCode
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithSuccessStatusCodeHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithSuccessStatusCode.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithSuccessStatusCode message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(Name = "custom-message-name")]
    public sealed partial record TestMessageWithName
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithNameHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithName.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithName message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(ApiGroupName = "Custom Message Group")]
    public sealed partial record TestMessageWithApiGroupName
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithApiGroupNameHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithApiGroupName.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithApiGroupName message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodGet)]
    public sealed partial record TestMessageWithGet
    {
        public required int Payload { get; init; }

        public required string Param { get; init; }
    }

    public sealed class TestMessageWithGetHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithGet.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithGet message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodGet)]
    public sealed partial record TestMessageWithGetWithoutPayload;

    public sealed class TestMessageWithGetWithoutPayloadHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithGetWithoutPayload.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithGetWithoutPayload message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = 11 };
        }
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodGet)]
    public sealed partial record TestMessageWithComplexGetPayload
    {
        [Required]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "set by ASP during model binding")]
        public int? Payload { get; init; }

        public required TestMessageWithComplexGetPayloadPayload NestedPayload { get; init; }

        public required List<int> NestedList { get; init; }

        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "for testing")]
        public required int[] NestedArray { get; init; }
    }

    public sealed record TestMessageWithComplexGetPayloadPayload
    {
        [Required]
        public required int Payload { get; init; }

        [Required]
        public required int Payload2 { get; init; }
    }

    public sealed class TestMessageWithComplexGetPayloadHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithComplexGetPayload.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithComplexGetPayload message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = (message.Payload ?? 0) + message.NestedPayload.Payload + message.NestedPayload.Payload2 + message.NestedList.Sum() + message.NestedArray.Sum() };
        }
    }

    [HttpMessage<TestMessageWithCustomSerializedPayloadTypeResponse>]
    public sealed partial record TestMessageWithCustomSerializedPayloadType
    {
        public required TestMessageWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    public sealed record TestMessageWithCustomSerializedPayloadTypeResponse
    {
        public required TestMessageWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    public sealed record TestMessageWithCustomSerializedPayloadTypePayload(int Payload);

    public sealed class TestMessageWithCustomSerializedPayloadTypeHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithCustomSerializedPayloadType.IHandler
    {
        public async Task<TestMessageWithCustomSerializedPayloadTypeResponse> Handle(TestMessageWithCustomSerializedPayloadType query,
                                                                                     CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = new(query.Payload.Payload + 1) };
        }

        internal sealed class PayloadJsonConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TestMessageWithCustomSerializedPayloadTypePayload);

            public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                return Activator.CreateInstance(typeof(PayloadJsonConverter)) as JsonConverter;
            }
        }

        internal sealed class PayloadJsonConverter : JsonConverter<TestMessageWithCustomSerializedPayloadTypePayload>
        {
            public override TestMessageWithCustomSerializedPayloadTypePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new(reader.GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, TestMessageWithCustomSerializedPayloadTypePayload value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Payload);
            }
        }
    }

    [HttpMessage<TestMessageWithCustomSerializerResponse>]
    public sealed partial record TestMessageWithCustomSerializer
    {
        public int PathPayload { get; init; }

        public int QueryPayload { get; init; }

        public int BodyPayload { get; init; }

        public static string FullPath => "/api/custom/path/for/serializer/{pathPayload:int}";

        public static IHttpMessageSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse> HttpMessageSerializer
            => new TestMessageCustomSerializer();

        public static IHttpResponseSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse> HttpResponseSerializer
            => new TestMessageCustomSerializer();
    }

    public sealed record TestMessageWithCustomSerializerResponse
    {
        public int Payload { get; init; }
    }

    private sealed class TestMessageCustomSerializer : IHttpMessageSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>,
                                                       IHttpResponseSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>
    {
        string IHttpMessageSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>.ContentType => "application/custom-message";

        string IHttpResponseSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>.ContentType => "application/custom-response";

        public async Task<(HttpContent? Content, string? Path, string? QueryString)> Serialize(IServiceProvider serviceProvider,
                                                                                               TestMessageWithCustomSerializer message, CancellationToken cancellationToken)
        {
            await Task.Yield();
            var httpContent = JsonContent.Create(new { message.BodyPayload }, new MediaTypeHeaderValue("application/custom-message"));
            return (httpContent, $"/api/custom/path/for/serializer/{message.PathPayload}", $"?query-payload={message.QueryPayload}");
        }

        public async Task<TestMessageWithCustomSerializer> Deserialize(IServiceProvider serviceProvider,
                                                                       Stream body,
                                                                       string path,
                                                                       IReadOnlyDictionary<string, IReadOnlyList<string?>>? query,
                                                                       CancellationToken cancellationToken)
        {
            await Task.Yield();
            using var reader = new StreamReader(body);
            var bodyContent = await reader.ReadToEndAsync(cancellationToken);
            var bodyMessage = JsonSerializer.Deserialize<TestMessageWithCustomSerializer>(bodyContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var pathParam = int.Parse(path.Trim('/').Replace("api/custom/path/for/serializer/", string.Empty));
            var queryParam = int.Parse(query!["query-payload"].Single()!);
            return new() { BodyPayload = bodyMessage!.BodyPayload, PathPayload = pathParam, QueryPayload = queryParam };
        }

        public async Task Serialize(IServiceProvider serviceProvider,
                                    Stream body,
                                    TestMessageWithCustomSerializerResponse response,
                                    CancellationToken cancellationToken)
        {
            await using var writer = new StreamWriter(body);
            await writer.WriteAsync($"{{\"total-payload\":{response.Payload}}}");
        }

        public async Task<TestMessageWithCustomSerializerResponse> Deserialize(IServiceProvider serviceProvider,
                                                                               HttpContent content,
                                                                               CancellationToken cancellationToken)
        {
            var stream = await content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken) as JsonObject;
            return new() { Payload = result!["total-payload"].Deserialize<int>() };
        }
    }

    public sealed class TestMessageWithCustomSerializerHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithCustomSerializer.IHandler
    {
        public async Task<TestMessageWithCustomSerializerResponse> Handle(TestMessageWithCustomSerializer message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.PathPayload + message.QueryPayload + message.BodyPayload };
        }
    }

    [HttpMessage<TestMessageWithCustomJsonTypeInfoResponse>]
    public sealed partial record TestMessageWithCustomJsonTypeInfo
    {
        public int MessagePayload { get; init; }

        public static JsonSerializerContext JsonSerializerContext => TestMessageWithCustomJsonTypeInfoJsonSerializerContext.Default;
    }

    public sealed record TestMessageWithCustomJsonTypeInfoResponse
    {
        public int ResponsePayload { get; init; }
    }

    public sealed class TestMessageWithCustomJsonTypeInfoHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithCustomJsonTypeInfo.IHandler
    {
        public async Task<TestMessageWithCustomJsonTypeInfoResponse> Handle(TestMessageWithCustomJsonTypeInfo message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { ResponsePayload = message.MessagePayload + 1 };
        }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
    [JsonSerializable(typeof(TestMessageWithCustomJsonTypeInfo))]
    [JsonSerializable(typeof(TestMessageWithCustomJsonTypeInfoResponse))]
    internal sealed partial class TestMessageWithCustomJsonTypeInfoJsonSerializerContext : JsonSerializerContext;

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithMiddleware
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithMiddlewareHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithMiddleware.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithMiddleware message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }

        public static void ConfigurePipeline(TestMessageWithMiddleware.IPipeline pipeline) =>
            pipeline.Use(pipeline.ServiceProvider.GetRequiredService<TestMessageMiddleware<TestMessageWithMiddleware, TestMessageResponse>>());
    }

    [HttpMessage]
    public sealed partial record TestMessageWithMiddlewareWithoutResponse
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithMiddlewareWithoutResponseHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithMiddlewareWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithMiddlewareWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }
        }

        public static void ConfigurePipeline(TestMessageWithMiddlewareWithoutResponse.IPipeline pipeline) =>
            pipeline.Use(pipeline.ServiceProvider.GetRequiredService<TestMessageMiddleware<TestMessageWithMiddlewareWithoutResponse, UnitMessageResponse>>());
    }

    public sealed class TestMessageMiddleware<TMessage, TResponse>(TestObservations observations) : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            observations.SeenTransportTypeInMiddleware = ctx.TransportType;
            return ctx.Next(ctx.Message, ctx.CancellationToken);
        }
    }

    [HttpMessage<TestMessageForAssemblyScanningResponse>]
    public sealed partial record TestMessageForAssemblyScanning
    {
        public int Payload { get; init; }
    }

    public sealed record TestMessageForAssemblyScanningResponse
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageForAssemblyScanningHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageForAssemblyScanning.IHandler
    {
        public async Task<TestMessageForAssemblyScanningResponse> Handle(TestMessageForAssemblyScanning message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }
    }

    [HttpMessage]
    public sealed partial record TestMessageWithoutResponseForAssemblyScanning
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithoutResponseForAssemblyScanningHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithoutResponseForAssemblyScanning.IHandler
    {
        public async Task Handle(TestMessageWithoutResponseForAssemblyScanning message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }
        }
    }

    [ApiController]
    private sealed class TestHttpMessageController : ControllerBase
    {
        [HttpPost("/custom/api/test", Name = nameof(TestMessage))]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType<TestMessageResponse>(200, MediaTypeNames.Application.Json)]
        public Task<TestMessageResponse> ExecuteTestMessage(TestMessage message, CancellationToken cancellationToken)
        {
            return HttpContext.GetMessageClient(TestMessage.T).Handle(message, cancellationToken);
        }

        [HttpPost("/custom/api/testMessageWithoutPayload", Name = nameof(TestMessageWithoutPayload))]
        [ProducesResponseType<TestMessageResponse>(200, MediaTypeNames.Application.Json)]
        public Task<TestMessageResponse> ExecuteTestMessageWithoutPayload(CancellationToken cancellationToken)
        {
            return HttpContext.GetMessageClient(TestMessageWithoutPayload.T).Handle(new(), cancellationToken);
        }

        [HttpPost("/custom/api/testMessageWithoutResponse", Name = nameof(TestMessageWithoutResponse))]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(200)]
        public Task ExecuteTestMessageWithoutResponse(TestMessageWithoutResponse message, CancellationToken cancellationToken)
        {
            return HttpContext.GetMessageClient(TestMessageWithoutResponse.T).Handle(message, cancellationToken);
        }

        [HttpPost("/custom/api/testMessageWithoutResponseWithoutPayload", Name = nameof(TestMessageWithoutResponseWithoutPayload))]
        [ProducesResponseType(200)]
        public Task ExecuteTestMessageWithoutPayloadWithoutResponse(CancellationToken cancellationToken)
        {
            return HttpContext.GetMessageClient(TestMessageWithoutResponseWithoutPayload.T).Handle(new(), cancellationToken);
        }

        [HttpGet("/custom/api/testMessageWithGet", Name = nameof(TestMessageWithGet))]
        [ProducesResponseType<TestMessageResponse>(200, MediaTypeNames.Application.Json)]
        public Task<TestMessageResponse> ExecuteTestMessageWithGet(int payload, string param, CancellationToken cancellationToken)
        {
            return HttpContext.GetMessageClient(TestMessageWithGet.T).Handle(new() { Payload = payload, Param = param }, cancellationToken);
        }

        [HttpGet("/custom/api/testMessageWithGetWithoutPayload", Name = nameof(TestMessageWithGetWithoutPayload))]
        [ProducesResponseType<TestMessageResponse>(200, MediaTypeNames.Application.Json)]
        public Task<TestMessageResponse> ExecuteTestMessageWithGetWithoutPayload(CancellationToken cancellationToken)
        {
            return HttpContext.GetMessageClient(TestMessageWithGetWithoutPayload.T).Handle(new(), cancellationToken);
        }

        [HttpPost("/custom/api/testMessageWithMiddleware", Name = nameof(TestMessageWithMiddleware))]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType<TestMessageResponse>(200, MediaTypeNames.Application.Json)]
        public Task<TestMessageResponse> ExecuteTestMessageWithMiddleware(TestMessageWithMiddleware message, CancellationToken cancellationToken)
        {
            return HttpContext.GetMessageClient(TestMessageWithMiddleware.T).Handle(message, cancellationToken);
        }
    }

    private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => nameof(TestControllerApplicationPart);

        public IEnumerable<TypeInfo> Types { get; } = [typeof(TestHttpMessageController).GetTypeInfo()];
    }

    private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpMessageController);
    }

    public sealed class TestObservations
    {
        public List<string?> ReceivedMessageIds { get; } = [];

        public List<string?> ReceivedTraceIds { get; } = [];

        public MessageTransportType? SeenTransportTypeInMiddleware { get; set; }

        public bool ShouldAddUpstreamData { get; set; }

        public bool ShouldAddBidirectionalData { get; set; }

        public IConquerorContextData? ReceivedDownstreamContextData { get; set; }

        public IConquerorContextData? ReceivedBidirectionalContextData { get; set; }
    }
}
