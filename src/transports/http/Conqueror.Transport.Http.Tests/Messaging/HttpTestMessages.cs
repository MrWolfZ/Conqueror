namespace Conqueror.Transport.Http.Tests.Messaging;

[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Members are used by ASP.NET Core via reflection")]
public static partial class HttpTestMessages
{
    public enum MessageTestCaseRegistrationMethod
    {
        Endpoints,
        ExplicitEndpoint,
    }

    public static void RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(this IServiceCollection services, MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler, IMessageHandlerWithSourceGeneration
    {
        _ = services.AddSingleton<TestObservations>();
        _ = services.AddMessageHandler<THandler>();

        _ = services.AddRouting()
                    .AddEndpointsApiExplorer()
                    .AddConquerorHttpServerAspNetCore();
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

            _ = endpoints.MapMessageEndpoint<TMessage, TResponse, TIHandler>();
        });
    }

    public sealed record MessageTestCase
    {
        public required Type MessageType { get; init; }

        public required Type? ResponseType { get; init; }

        public required Type HandlerType { get; init; }

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

        public required object Message { get; init; }

        public required object? Response { get; init; }
    }

    public sealed record TestMessageResponse
    {
        public required int Payload { get; init; }
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
