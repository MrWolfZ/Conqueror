// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

using System.Text;
using Microsoft.AspNetCore.Builder;

#pragma warning disable SA1302
#pragma warning disable CA1715

namespace Conqueror.Transport.Http.Tests.Messaging;

[SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "emulating generated code")]
[SuppressMessage("ReSharper", "PreferConcreteValueOverDefault", Justification = "emulating generated code")]
[SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract", Justification = "emulating generated code")]
public sealed partial class MessageTypeGenerationTests
{
    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "the closure is evaluated before disposal")]
    public async Task GivenMessageTypeWithExplicitImplementations_WhenUsingHandler_ItWorks()
    {
        await using var host = await TestHost.Create(
            services =>
            {
                _ = services.AddMvc().AddConquerorMessageControllers();

                _ = services.AddConquerorMessageHandler<TestMessageHandler>()
                            .AddConquerorMessageHandler<TestMessageWithoutResponseHandler>()
                            .BuildServiceProvider();
            },
            app =>
            {
                _ = app.UseRouting();
                _ = app.UseConqueror();
                _ = app.UseEndpoints(b => b.MapControllers());
            });

        var messageClients = host.Resolve<IMessageClients>();

        var result = await messageClients.For(TestMessage.T)
                                         .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(host.HttpClient))
                                         .Handle(new() { Payload = 10 });

        Assert.That(result, Is.Not.Null);

        await messageClients.For(TestMessageWithoutResponse.T)
                            .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(host.HttpClient))
                            .Handle(new("test"));
    }

    [HttpMessage]
    [Message<TestMessageResponse>]
    private sealed partial record TestMessage
    {
        public required int Payload { get; init; }
    }

    private sealed record TestMessageResponse;

    // generated http
    private sealed partial record TestMessage : IHttpMessage<TestMessage, TestMessageResponse>
    {
        static string IHttpMessage<TestMessage, TestMessageResponse>.HttpMethod => "GET";

        static IHttpMessageTypesInjector IHttpMessage.HttpMessageTypesInjector
            => HttpMessageTypesInjector<TestMessage, TestMessageResponse>.Default;

        static IHttpMessageSerializer<TestMessage, TestMessageResponse> IHttpMessage<TestMessage, TestMessageResponse>.HttpMessageSerializer
            => new HttpMessageQueryStringSerializer<TestMessage, TestMessageResponse>(
                query =>
                {
                    if (query is null)
                    {
                        throw new ArgumentException("query must not be null", nameof(query));
                    }

                    return new()
                    {
                        Payload = query.TryGetValue("payload", out var payloadValues) && payloadValues.Count > 0 && payloadValues[0] is { } payloadValue ? (int)Convert.ChangeType(payloadValue, typeof(int)) : default,
                    };
                },
                message =>
                {
                    var queryBuilder = new StringBuilder();

                    queryBuilder.Append('?');
                    queryBuilder.Append("payload=");
                    queryBuilder.Append(Uri.EscapeDataString(message.Payload.ToString() ?? string.Empty));

                    return queryBuilder.ToString();
                });
    }

    private sealed class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return new();
        }
    }

    [HttpMessage]
    [Message]
    private sealed partial record TestMessageWithoutResponse(string Payload);

    // generated http
    private sealed partial record TestMessageWithoutResponse : IHttpMessage<TestMessageWithoutResponse>
    {
        static IHttpMessageTypesInjector IHttpMessage.HttpMessageTypesInjector
            => HttpMessageTypesInjector<TestMessageWithoutResponse, UnitMessageResponse>.Default;
    }

    private sealed class TestMessageWithoutResponseHandler : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }
    }

    private interface ICustomHttpMessage<TMessage> : IHttpMessage<TMessage, TestMessageResponse>
        where TMessage : class, IHttpMessage<TMessage, TestMessageResponse>
    {
        static string IHttpMessage<TMessage, TestMessageResponse>.Name => "test";
    }

    private sealed partial record TestMessageWithCustomInterface(string Payload) : ICustomHttpMessage<TestMessageWithCustomInterface>;

    // generated http
    // ReSharper disable once RedundantExtendsListEntry
    private sealed partial record TestMessageWithCustomInterface : IHttpMessage<TestMessageWithCustomInterface, TestMessageResponse>
    {
        static IHttpMessageTypesInjector IHttpMessage.HttpMessageTypesInjector
            => HttpMessageTypesInjector<TestMessageWithCustomInterface, TestMessageResponse>.Default;

        public static IDefaultMessageTypesInjector DefaultTypeInjector => throw new NotSupportedException();

        public static IReadOnlyCollection<IMessageTypesInjector> TypeInjectors => [];

        public static MessageTypes<TestMessageWithCustomInterface, TestMessageResponse> T => MessageTypes<TestMessageWithCustomInterface, TestMessageResponse>.Default;

        public static TestMessageWithCustomInterface? EmptyInstance { get; }
    }
}
