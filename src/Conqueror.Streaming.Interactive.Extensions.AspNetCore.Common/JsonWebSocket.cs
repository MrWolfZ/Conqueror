using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common
{
    internal sealed class JsonWebSocket : IDisposable
    {
        private readonly JsonSerializerOptions jsonSerializerOptions;
        private readonly TextWebSocketWithHeartbeat socket;

        public JsonWebSocket(TextWebSocketWithHeartbeat socket, JsonSerializerOptions jsonSerializerOptions)
        {
            this.socket = socket;
            this.jsonSerializerOptions = jsonSerializerOptions;
        }

        public event OnSocketClose? SocketClosed
        {
            add => socket.SocketClosed += value;
            remove => socket.SocketClosed -= value;
        }

        public async Task<object?> Receive(string discriminatorPropertyName, Func<string, Type> messageTypeLookup, CancellationToken cancellationToken)
        {
            var msg = await socket.Receive(cancellationToken);

            if (msg == null)
            {
                return null;
            }

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

            return parsed.Deserialize(messageType, jsonSerializerOptions);
        }

        public async Task Send(object message, CancellationToken cancellationToken)
        {
            await socket.Send(JsonSerializer.Serialize(message, jsonSerializerOptions), cancellationToken);
        }

        public async Task Close(CancellationToken cancellationToken)
        {
            await socket.Close(cancellationToken);
        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}
