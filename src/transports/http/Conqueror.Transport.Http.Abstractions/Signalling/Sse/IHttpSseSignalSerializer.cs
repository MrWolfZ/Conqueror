using System;
using System.Buffers;

// ReSharper disable once CheckNamespace
namespace Conqueror;

// TODO: make this public once the API is more stable
internal interface IHttpSseSignalSerializer<TSignal>
    where TSignal : class, IHttpSseSignal<TSignal>
{
    void Serialize(
        IServiceProvider serviceProvider,
        TSignal signal,
        IBufferWriter<byte> bufferWriter);

    TSignal Deserialize(
        IServiceProvider serviceProvider,
        string serializedSignal);
}
