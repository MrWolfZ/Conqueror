namespace Conqueror.Middleware.Authorization.Tests.AOT;

using System.Text.Json.Serialization;

[Message<TestMessageResponse>]
internal sealed partial record TestMessage
{
    public required int Payload { get; init; }
}

internal sealed record TestMessageResponse(int Payload);

internal sealed partial class TestMessageHandler : TestMessage.IHandler
{
    public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        return new TestMessageResponse(message.Payload + 1);
    }

    public static void ConfigurePipeline(TestMessage.IPipeline pipeline) =>
        pipeline.UseAuthorization(c => c
            .AddAuthorizationCheck("test", ctx => ctx.Success()));
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TestMessage))]
[JsonSerializable(typeof(TestMessageResponse))]
internal sealed partial class TestMessageJsonSerializerContext : JsonSerializerContext;
