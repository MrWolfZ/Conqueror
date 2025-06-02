using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

// TODO: make this public once the API is more stable
internal interface IHttpWebSocketsSignalSerializer<TSignal>
    where TSignal : class, IHttpWebSocketsSignal<TSignal>
{
    ValueTask Serialize(IServiceProvider serviceProvider, TSignal signal, Stream stream, CancellationToken cancellationToken);

    ValueTask<TSignal> Deserialize(IServiceProvider serviceProvider, Stream stream, CancellationToken cancellationToken);
}
