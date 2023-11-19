using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore;

internal sealed class WebSocketsTransportPublisher : IConquerorEventTransportPublisher<WebSocketsEventAttribute>
{
    private readonly ConcurrentDictionary<Guid, ChannelWriter<object>> readers = new();

    public async Task PublishEvent<TEvent>(TEvent evt, WebSocketsEventAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var writers = readers.Values.ToList();

        foreach (var writer in writers)
        {
            await writer.WriteAsync(evt, cancellationToken).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<object> GetEvents(IReadOnlyCollection<string> eventTypeIds, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<object>(new BoundedChannelOptions(8));

        _ = readers.AddOrUpdate(id, _ => channel.Writer, (_, _) => channel.Writer);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return evt;
            }
        }
        finally
        {
            if (readers.TryRemove(id, out var writer))
            {
                writer.Complete();
            }
        }
    }
}
