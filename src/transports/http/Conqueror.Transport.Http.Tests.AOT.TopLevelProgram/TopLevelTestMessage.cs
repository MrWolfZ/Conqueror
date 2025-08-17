namespace Conqueror.Transport.Http.Tests.AOT.TopLevelProgram;

using System.Text.Json.Serialization;

[HttpMessage<TopLevelTestMessageResponse>(
    PathPrefix = "api/prefix",
    Version = "v1",
    Path = "messages/test",
    ApiGroupName = "Test Messages",
    Name = "test-message-name"
)]
internal sealed partial record TopLevelTestMessage
{
    public required int Payload { get; init; }

    public required NestedObject Nested { get; init; }
}

internal sealed record NestedObject
{
    public required string NestedString { get; init; }
}

internal sealed record TopLevelTestMessageResponse(int Payload);

internal sealed partial class TopLevelTestMessageHandler : TopLevelTestMessage.IHandler
{
    public async Task<TopLevelTestMessageResponse> Handle(
        TopLevelTestMessage message,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Yield();

        return new TopLevelTestMessageResponse(message.Payload + 1);
    }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TopLevelTestMessage))]
[JsonSerializable(typeof(TopLevelTestMessageResponse))]
internal sealed partial class TopLevelTestMessageJsonSerializerContext : JsonSerializerContext;
