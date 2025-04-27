using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Conqueror.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
        Endpoints,
        ExplicitEndpoint,
    }

    public static void RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(this IServiceCollection services, MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler
    {
        _ = services.AddSingleton<TestObservations>()
                    .AddTransient(typeof(TestMessageMiddleware<,>));

        if (typeof(THandler) == typeof(TIHandler))
        {
            _ = services.AddMessageHandlerDelegate(new MessageTypes<TMessage, TResponse, TIHandler>(),
                                                   async (_, _, _) =>
                                                   {
                                                       await Task.Yield();
                                                       throw new NotSupportedException();
                                                   });
        }
        else if (typeof(TMessage) == typeof(TestMessageForAssemblyScanning)
                 || typeof(TMessage) == typeof(TestMessageWithoutResponseForAssemblyScanning))
        {
            _ = services.AddMessageHandlersFromAssembly(typeof(TestMessageForAssemblyScanning).Assembly);
        }
        else
        {
            _ = services.AddMessageHandler<THandler>();
        }

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

        _ = services.AddRouting()
                    .AddEndpointsApiExplorer()
                    .AddMessageEndpoints();
    }

    public static void MapMessageEndpoints<TMessage, TResponse, TIHandler>(this IApplicationBuilder app, MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        _ = app.UseConquerorWellKnownErrorHandling();
        _ = app.UseRouting();

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

            // delegate handlers are not supported
            if (testCase.MessageType == typeof(TestMessageWithDelegateHandler))
            {
                return;
            }

            _ = endpoints.MapMessageEndpoint<TMessage, TResponse, TIHandler>();
        });
    }

    public static IEnumerable<TestCaseData> GenerateTestCaseData()
    {
        return GenerateTestCases().Select(c => new TestCaseData(c)
        {
            TypeArgs = [c.MessageType, c.ResponseType ?? typeof(UnitMessageResponse), c.IHandlerType, c.HandlerType ?? c.IHandlerType],
        });
    }

    private static IEnumerable<MessageTestCase> GenerateTestCases()
    {
        foreach (var registrationMethod in new[]
                 {
                     MessageTestCaseRegistrationMethod.Endpoints,
                     MessageTestCaseRegistrationMethod.ExplicitEndpoint,
                 })
        {
            yield return new()
            {
                MessageType = typeof(TestMessage),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageHandler),
                IHandlerType = typeof(TestMessage.IHandler),
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
                IHandlerType = typeof(TestMessageWithoutResponse.IHandler),
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
                IHandlerType = typeof(TestMessageWithoutPayload.IHandler),
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
                IHandlerType = typeof(TestMessageWithoutResponseWithoutPayload.IHandler),
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
                IHandlerType = typeof(TestMessageWithMethod.IHandler),
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
                IHandlerType = typeof(TestMessageWithPathPrefix.IHandler),
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
                IHandlerType = typeof(TestMessageWithVersion.IHandler),
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
                IHandlerType = typeof(TestMessageWithPath.IHandler),
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
                IHandlerType = typeof(TestMessageWithPathPrefixAndPathAndVersion.IHandler),
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
                IHandlerType = typeof(TestMessageWithFullPath.IHandler),
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
                IHandlerType = typeof(TestMessageWithFullPathAndVersion.IHandler),
                HttpMethod = MethodPost,
                FullPath = "/custom/full/path/for/message/ignoring/version",
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
                IHandlerType = typeof(TestMessageWithSuccessStatusCode.IHandler),
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
                IHandlerType = typeof(TestMessageWithName.IHandler),
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
                IHandlerType = typeof(TestMessageWithApiGroupName.IHandler),
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
                IHandlerType = typeof(TestMessageWithGet.IHandler),
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
                IHandlerType = typeof(TestMessageWithGetWithoutPayload.IHandler),
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

            yield return new()
            {
                MessageType = typeof(TestMessageWithGetWithOptionalPayload),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithGetWithOptionalPayloadHandler),
                IHandlerType = typeof(TestMessageWithGetWithOptionalPayload.IHandler),
                HttpMethod = MethodGet,
                FullPath = "/api/testMessageWithGetWithOptionalPayload",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 2,
                QueryString = null,
                Payload = null,
                ResponsePayload = "{\"payload\":1}",
                MessageContentType = null,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithGetWithOptionalPayload(),
                Response = new TestMessageResponse { Payload = 1 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithGetWithPrimaryConstructor),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithGetWithPrimaryConstructorHandler),
                IHandlerType = typeof(TestMessageWithGetWithPrimaryConstructor.IHandler),
                HttpMethod = MethodGet,
                FullPath = "/api/testMessageWithGetWithPrimaryConstructor",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 3,
                QueryString = "?payload=10&param=test&intArray=11&intArray=12",
                Payload = null,
                ResponsePayload = "{\"payload\":33}",
                MessageContentType = null,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithGetWithPrimaryConstructor(10, "test", [11, 12]),
                Response = new TestMessageResponse { Payload = 33 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithGetWithPrimaryConstructorWithOptionalParameters),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithGetWithPrimaryConstructorWithOptionalParametersHandler),
                IHandlerType = typeof(TestMessageWithGetWithPrimaryConstructorWithOptionalParameters.IHandler),
                HttpMethod = MethodGet,
                FullPath = "/api/testMessageWithGetWithPrimaryConstructorWithOptionalParameters",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 2,
                QueryString = null,
                Payload = null,
                ResponsePayload = "{\"payload\":1}",
                MessageContentType = null,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithGetWithPrimaryConstructorWithOptionalParameters(),
                Response = new TestMessageResponse { Payload = 1 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithComplexGetPayload),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithComplexGetPayloadHandler),
                IHandlerType = typeof(TestMessageWithComplexGetPayload.IHandler),
                HttpMethod = MethodGet,
                FullPath = "/api/testMessageWithComplexGetPayload",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 3,
                QueryString = "?payload=10&nestedList=11&nestedList=12&nestedArray=13&nestedArray=14",
                Payload = null,
                ResponsePayload = "{\"payload\":60}",
                MessageContentType = null,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithComplexGetPayload
                {
                    Payload = 10,
                    NestedList = [11, 12],
                    NestedArray = [13, 14],
                },
                Response = new TestMessageResponse { Payload = 60 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithCustomSerializedPayloadType),
                ResponseType = typeof(TestMessageWithCustomSerializedPayloadTypeResponse),
                HandlerType = typeof(TestMessageWithCustomSerializedPayloadTypeHandler),
                IHandlerType = typeof(TestMessageWithCustomSerializedPayloadType.IHandler),
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

            yield return new()
            {
                MessageType = typeof(TestMessageWithCustomSerializer),
                ResponseType = typeof(TestMessageWithCustomSerializerResponse),
                HandlerType = typeof(TestMessageWithCustomSerializerHandler),
                IHandlerType = typeof(TestMessageWithCustomSerializer.IHandler),
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
                IHandlerType = typeof(TestMessageWithCustomJsonTypeInfo.IHandler),
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
                JsonSerializerContext = GetJsonSerializerContext<TestMessageWithCustomJsonTypeInfo, TestMessageWithCustomJsonTypeInfoResponse>(),
                Message = new TestMessageWithCustomJsonTypeInfo { MessagePayload = 10 },
                Response = new TestMessageWithCustomJsonTypeInfoResponse { ResponsePayload = 11 },
                RegistrationMethod = registrationMethod,
            };

            static JsonSerializerContext? GetJsonSerializerContext<TMessage, TResponse>()
                where TMessage : class, IMessage<TMessage, TResponse>
                => TMessage.JsonSerializerContext;

            yield return new()
            {
                MessageType = typeof(TestMessageWithMiddleware),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithMiddlewareHandler),
                IHandlerType = typeof(TestMessageWithMiddleware.IHandler),
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
                IHandlerType = typeof(TestMessageWithMiddlewareWithoutResponse.IHandler),
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

            yield return new()
            {
                MessageType = typeof(TestMessageWithArrayResponse),
                ResponseType = typeof(TestMessageResponse[]),
                HandlerType = typeof(TestMessageWithArrayResponseHandler),
                IHandlerType = typeof(TestMessageWithArrayResponse.IHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithArrayResponse",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "[{\"payload\":11},{\"payload\":12}]",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithArrayResponse { Payload = 10 },
                Response = new TestMessageResponse[] { new() { Payload = 11 }, new() { Payload = 12 } },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithListResponse),
                ResponseType = typeof(List<TestMessageResponse>),
                HandlerType = typeof(TestMessageWithListResponseHandler),
                IHandlerType = typeof(TestMessageWithListResponse.IHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithListResponse",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "[{\"payload\":11},{\"payload\":12}]",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithListResponse { Payload = 10 },
                Response = new List<TestMessageResponse> { new() { Payload = 11 }, new() { Payload = 12 } },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithEnumerableResponse),
                ResponseType = typeof(IEnumerable<TestMessageResponse>),
                HandlerType = typeof(TestMessageWithEnumerableResponseHandler),
                IHandlerType = typeof(TestMessageWithEnumerableResponse.IHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithEnumerableResponse",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "[{\"payload\":11},{\"payload\":12}]",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithEnumerableResponse { Payload = 10 },
                Response = new List<TestMessageResponse> { new() { Payload = 11 }, new() { Payload = 12 } },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageOmittedFromApiDescription),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageOmittedFromApiDescriptionHandler),
                IHandlerType = typeof(TestMessageOmittedFromApiDescription.IHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageOmittedFromApiDescription",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                IsOmittedFromApiDescriptions = true,
                Message = new TestMessageOmittedFromApiDescription { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithDisabledHandler),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithDisabledHandlerHandler),
                IHandlerType = typeof(TestMessageWithDisabledHandler.IHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithDisabledHandler",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = string.Empty,
                ResponsePayload = string.Empty,
                MessageContentType = null,
                ResponseContentType = null,
                HandlerIsEnabled = false,
                Message = new TestMessageWithDisabledHandler { Payload = 10 },
                Response = null,
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithoutResponseWithDisabledHandler),
                ResponseType = null,
                HandlerType = typeof(TestMessageWithoutResponseWithDisabledHandlerHandler),
                IHandlerType = typeof(TestMessageWithoutResponseWithDisabledHandler.IHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithoutResponseWithDisabledHandler",
                SuccessStatusCode = 204,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = string.Empty,
                ResponsePayload = string.Empty,
                MessageContentType = null,
                ResponseContentType = null,
                HandlerIsEnabled = false,
                Message = new TestMessageWithoutResponseWithDisabledHandler { Payload = 10 },
                Response = null,
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithDelegateHandler),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = null,
                IHandlerType = typeof(TestMessageWithDelegateHandler.IHandler),
                HttpMethod = MethodPost,
                FullPath = "/api/testMessageWithDelegateHandler",
                SuccessStatusCode = 200,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = string.Empty,
                ResponsePayload = string.Empty,
                MessageContentType = null,
                ResponseContentType = null,
                HandlerIsEnabled = false,
                Message = new TestMessageWithDelegateHandler { Payload = 10 },
                Response = null,
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageWithCustomConventions),
                ResponseType = typeof(TestMessageResponse),
                HandlerType = typeof(TestMessageWithCustomConventionsHandler),
                IHandlerType = typeof(TestMessageWithCustomConventions.IHandler),
                HttpMethod = MethodPost,
                FullPath = "/customApi/testMessageWithCustomConventions",
                SuccessStatusCode = 201,
                ApiGroupName = null,
                Name = null,
                ParameterCount = 1,
                QueryString = null,
                Payload = "{\"payload\":10}",
                ResponsePayload = "{\"payload\":11}",
                MessageContentType = MediaTypeNames.Application.Json,
                ResponseContentType = MediaTypeNames.Application.Json,
                Message = new TestMessageWithCustomConventions { Payload = 10 },
                Response = new TestMessageResponse { Payload = 11 },
                RegistrationMethod = registrationMethod,
            };

            yield return new()
            {
                MessageType = typeof(TestMessageForAssemblyScanning),
                ResponseType = typeof(TestMessageForAssemblyScanningResponse),
                HandlerType = typeof(TestMessageForAssemblyScanningHandler),
                IHandlerType = typeof(TestMessageForAssemblyScanning.IHandler),
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
                IHandlerType = typeof(TestMessageWithoutResponseForAssemblyScanning.IHandler),
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

        public required Type? HandlerType { get; init; }

        public required Type IHandlerType { get; init; }

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

        public bool HandlerIsEnabled { get; init; } = true;

        public bool IsOmittedFromApiDescriptions { get; init; }

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

    public sealed partial class TestMessageHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class DisabledTestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw new InvalidOperationException("This handler should not be called.");
        }

        static void IHttpMessageHandler.ConfigureHttpReceiver(IHttpMessageReceiver receiver)
            => receiver.Disable();
    }

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithoutPayload;

    public sealed partial class TestMessageWithoutPayloadHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithoutResponseHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class DisabledTestMessageWithoutResponseHandler : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw new InvalidOperationException("This handler should not be called.");
        }

        static void IHttpMessageHandler.ConfigureHttpReceiver(IHttpMessageReceiver receiver)
            => receiver.Disable();
    }

    [HttpMessage]
    public sealed partial record TestMessageWithoutResponseWithoutPayload;

    public sealed partial class TestMessageWithoutResponseWithoutPayloadHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithMethodHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithPathPrefixHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithVersionHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithPathHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithPathPrefixAndPathAndVersionHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithFullPathHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    [HttpMessage<TestMessageResponse>(FullPath = "/custom/full/path/for/message/ignoring/version", Version = "v2")]
    public sealed partial record TestMessageWithFullPathAndVersion
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestMessageWithFullPathAndVersionHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithSuccessStatusCodeHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithNameHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithApiGroupNameHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithGetHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithGetWithoutPayloadHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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
    public sealed partial record TestMessageWithGetWithOptionalPayload
    {
        public int? Payload { get; init; }

        public string? Param { get; init; }
    }

    public sealed partial class TestMessageWithGetWithOptionalPayloadHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithGetWithOptionalPayload.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithGetWithOptionalPayload message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = (message.Payload ?? 0) + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodGet)]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "testing")]
    public sealed partial record TestMessageWithGetWithPrimaryConstructor(int Payload, string Param, int[] IntArray);

    public sealed partial class TestMessageWithGetWithPrimaryConstructorHandler(
        IServiceProvider serviceProvider,
        FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithGetWithPrimaryConstructor.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithGetWithPrimaryConstructor message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + message.IntArray.Sum() };
        }
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodGet)]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "testing")]
    public sealed partial record TestMessageWithGetWithPrimaryConstructorWithOptionalParameters(int? Payload = null, string? Param = null);

    public sealed partial class TestMessageWithGetWithPrimaryConstructorWithOptionalParametersHandler(
        IServiceProvider serviceProvider,
        FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithGetWithPrimaryConstructorWithOptionalParameters.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithGetWithPrimaryConstructorWithOptionalParameters message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = (message.Payload ?? 0) + 1 };
        }
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodGet)]
    public sealed partial record TestMessageWithComplexGetPayload
    {
        [Required]
        public int? Payload { get; init; }

        public required List<int> NestedList { get; init; }

        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "for testing")]
        public required int[] NestedArray { get; init; }
    }

    public sealed partial class TestMessageWithComplexGetPayloadHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

            return new() { Payload = (message.Payload ?? 0) + message.NestedList.Sum() + message.NestedArray.Sum() };
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

    public sealed partial class TestMessageWithCustomSerializedPayloadTypeHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithCustomSerializedPayloadType.IHandler
    {
        public async Task<TestMessageWithCustomSerializedPayloadTypeResponse> Handle(TestMessageWithCustomSerializedPayloadType message,
                                                                                     CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = new(message.Payload.Payload + 1) };
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

    public sealed partial class TestMessageWithCustomSerializerHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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
    }

    public sealed record TestMessageWithCustomJsonTypeInfoResponse
    {
        public int ResponsePayload { get; init; }
    }

    public sealed partial class TestMessageWithCustomJsonTypeInfoHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithMiddlewareHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithMiddlewareWithoutResponseHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    [HttpMessage<TestMessageResponse[]>]
    public sealed partial record TestMessageWithArrayResponse
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestMessageWithArrayResponseHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithArrayResponse.IHandler
    {
        public async Task<TestMessageResponse[]> Handle(TestMessageWithArrayResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return [new() { Payload = message.Payload + 1 }, new() { Payload = message.Payload + 2 }];
        }
    }

    [HttpMessage<List<TestMessageResponse>>]
    public sealed partial record TestMessageWithListResponse
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestMessageWithListResponseHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithListResponse.IHandler
    {
        public async Task<List<TestMessageResponse>> Handle(TestMessageWithListResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return [new() { Payload = message.Payload + 1 }, new() { Payload = message.Payload + 2 }];
        }
    }

    [HttpMessage<IEnumerable<TestMessageResponse>>]
    public sealed partial record TestMessageWithEnumerableResponse
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestMessageWithEnumerableResponseHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithEnumerableResponse.IHandler
    {
        public async Task<IEnumerable<TestMessageResponse>> Handle(TestMessageWithEnumerableResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return [new() { Payload = message.Payload + 1 }, new() { Payload = message.Payload + 2 }];
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

    public sealed partial class TestMessageForAssemblyScanningHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    public sealed partial class TestMessageWithoutResponseForAssemblyScanningHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
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

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessageOmittedFromApiDescription
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestMessageOmittedFromApiDescriptionHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageOmittedFromApiDescription.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageOmittedFromApiDescription message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }

        static void IHttpMessageHandler.ConfigureHttpReceiver(IHttpMessageReceiver receiver)
        {
            _ = receiver.OmitFromApiDescription();
        }
    }

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithDisabledHandler
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestMessageWithDisabledHandlerHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithDisabledHandler.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithDisabledHandler message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }

            return new() { Payload = message.Payload + 1 };
        }

        static void IHttpMessageHandler.ConfigureHttpReceiver(IHttpMessageReceiver receiver)
        {
            _ = receiver.Disable();
        }
    }

    [HttpMessage]
    public sealed partial record TestMessageWithoutResponseWithDisabledHandler
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestMessageWithoutResponseWithDisabledHandlerHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithoutResponseWithDisabledHandler.IHandler
    {
        public async Task Handle(TestMessageWithoutResponseWithDisabledHandler message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(serviceProvider);
            }
        }

        static void IHttpMessageHandler.ConfigureHttpReceiver(IHttpMessageReceiver receiver)
        {
            _ = receiver.Disable();
        }
    }

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithDelegateHandler
    {
        public int Payload { get; init; }
    }

    [CustomHttpMessage<TestMessageResponse>(CustomPathPrefix = "customApi")]
    public sealed partial record TestMessageWithCustomConventions
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestMessageWithCustomConventionsHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestMessageWithCustomConventions.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithCustomConventions message, CancellationToken cancellationToken = default)
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

[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "used by source generator")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "used by source generator")]
[MessageTransport(Prefix = "Http", Namespace = "Conqueror", FullyQualifiedMessageTypeName = "Conqueror.Transport.Http.Tests.Messaging.ICustomHttpMessage")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CustomHttpMessageAttribute<TResponse> : Attribute
{
    public string? CustomPathPrefix { get; set; }
}

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "The static members are intentionally per generic type")]
[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Members are set via code generation")]
public interface ICustomHttpMessage<TMessage, TResponse> : IHttpMessage<TMessage, TResponse>
    where TMessage : class, ICustomHttpMessage<TMessage, TResponse>
{
    static string IHttpMessage<TMessage, TResponse>.PathPrefix => TMessage.CustomPathPrefix ?? "api";

    static int IHttpMessage<TMessage, TResponse>.SuccessStatusCode => 201;

    static virtual string? CustomPathPrefix { get; }
}
