using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Transport.Http.Common;

internal sealed class JsonWebSocket(TextWebSocketWithHeartbeat socket, JsonSerializerOptions jsonSerializerOptions) : IDisposable
{
    public void Dispose()
    {
        socket.Dispose();
    }

    public async IAsyncEnumerable<object> Read(string discriminatorPropertyName,
                                               Func<string, Type> messageTypeLookup,
                                               [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var msg in socket.Read(cancellationToken).ConfigureAwait(false))
        {
            var parsed = JsonSerializer.Deserialize<JsonObject>(msg, jsonSerializerOptions);

            if (parsed == null)
            {
                throw new InvalidDataException($"message '{msg}' could not be deserialized as json");
            }

            if (!parsed.TryGetPropertyValue(discriminatorPropertyName, out var discriminatorProperty) || discriminatorProperty is null)
            {
                throw new InvalidDataException($"message '{msg}' does not have discriminator property '{discriminatorPropertyName}'");
            }

            var discriminatorValue = discriminatorProperty.GetValue<string>();
            var messageType = messageTypeLookup(discriminatorValue);

            var deserialized = parsed.Deserialize(messageType, jsonSerializerOptions);

            if (deserialized is null)
            {
                throw new InvalidDataException($"message '{msg}' could not be deserialized into message type '{messageType}'");
            }

            yield return deserialized;
        }
    }

    public async Task<bool> Send(object message, CancellationToken cancellationToken)
    {
        return await socket.Send(JsonSerializer.Serialize(message, jsonSerializerOptions), cancellationToken).ConfigureAwait(false);
    }

    public async Task Close(CancellationToken cancellationToken)
    {
        await socket.Close(cancellationToken).ConfigureAwait(false);
    }
}
