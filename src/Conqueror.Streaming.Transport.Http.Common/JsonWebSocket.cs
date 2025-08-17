namespace Conqueror.Streaming.Transport.Http.Common;

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

internal sealed class JsonWebSocket(TextWebSocketWithHeartbeat socket, JsonSerializerOptions jsonSerializerOptions)
    : IDisposable
{
    public void Dispose() => socket.Dispose();

    public async IAsyncEnumerable<object> Read(
        string discriminatorPropertyName,
        Func<string, Type> messageTypeLookup,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await foreach (var msg in socket.Read(cancellationToken).ConfigureAwait(false))
        {
            var parsed =
                JsonSerializer.Deserialize<JsonObject>(msg, jsonSerializerOptions)
                ?? throw new InvalidDataException($"message '{msg}' could not be deserialized as json");

            if (
                !parsed.TryGetPropertyValue(discriminatorPropertyName, out var discriminatorProperty)
                || discriminatorProperty is null
            )
            {
                throw new InvalidDataException(
                    $"message '{msg}' does not have discriminator property '{discriminatorPropertyName}'"
                );
            }

            var discriminatorValue = discriminatorProperty.GetValue<string>();
            var messageType = messageTypeLookup(discriminatorValue);

            var deserialized =
                parsed.Deserialize(messageType, jsonSerializerOptions)
                ?? throw new InvalidDataException(
                    $"message '{msg}' could not be deserialized into message type '{messageType}'"
                );

            yield return deserialized;
        }
    }

    public Task<bool> Send(object message, CancellationToken cancellationToken) =>
        socket.Send(JsonSerializer.Serialize(message, jsonSerializerOptions), cancellationToken);

    public async Task Close(CancellationToken cancellationToken) =>
        await socket.Close(cancellationToken).ConfigureAwait(false);
}
