using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common.Transport.WebSockets;

namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore;

internal sealed class EventingServerWebSocket : IDisposable
{
    private readonly JsonWebSocket socket;

    public EventingServerWebSocket(JsonWebSocket socket)
    {
        this.socket = socket;
    }

    public void Dispose()
    {
        socket.Dispose();
    }

    public IAsyncEnumerable<object> Read(CancellationToken cancellationToken)
    {
        return socket.Read("type", LookupMessageType, cancellationToken);

        static Type LookupMessageType(string discriminator) => discriminator switch
        {
            _ => throw new ArgumentOutOfRangeException(nameof(discriminator), discriminator, null),
        };
    }

    public async Task<bool> SendMessage(object message, CancellationToken cancellationToken)
    {
        var attribute = message.GetType().GetCustomAttribute<WebSocketsEventAttribute>();
        var eventTypeId = attribute?.EventTypeId ?? message.GetType().FullName ?? message.GetType().Name;
        
        return await socket.Send(new MessageEnvelope(MessageEnvelope.Discriminator, eventTypeId, message), cancellationToken).ConfigureAwait(false);
    }

    public async Task Close(CancellationToken cancellationToken) => await socket.Close(cancellationToken).ConfigureAwait(false);
}

internal sealed record MessageEnvelope(string Type, string EventTypeId, object Message)
{
    public const string Discriminator = "envelope";
}
