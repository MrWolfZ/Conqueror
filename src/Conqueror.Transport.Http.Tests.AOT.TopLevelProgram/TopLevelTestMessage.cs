using System.Text.Json.Serialization;

namespace Conqueror.Transport.Http.Tests.AOT.TopLevelProgram;

[HttpMessage(Path = "test")]
public sealed partial record TopLevelTestMessage : IMessage<TopLevelTestMessageResponse>
{
    public required int Payload { get; init; }

    public required NestedObject Nested { get; init; }

    public static JsonSerializerContext JsonSerializerContext => TopLevelTestMessageJsonSerializerContext.Default;
}

public sealed record NestedObject
{
    public required string NestedString { get; init; }
}

public sealed record TopLevelTestMessageResponse(int Payload);

internal sealed class TopLevelTestMessageHandler : TopLevelTestMessage.IHandler
{
    public async Task<TopLevelTestMessageResponse> Handle(TopLevelTestMessage command, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new(command.Payload + 1);
    }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TopLevelTestMessage))]
[JsonSerializable(typeof(TopLevelTestMessageResponse))]
internal sealed partial class TopLevelTestMessageJsonSerializerContext : JsonSerializerContext;
