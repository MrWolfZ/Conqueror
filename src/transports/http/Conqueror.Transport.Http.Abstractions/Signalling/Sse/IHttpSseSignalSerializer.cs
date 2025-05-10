using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

// TODO: make this public once the API is more stable
internal interface IHttpSseSignalSerializer<TSignal>
    where TSignal : class, IHttpSseSignal<TSignal>
{
    string Serialize(IServiceProvider serviceProvider, TSignal signal);

    TSignal Deserialize(IServiceProvider serviceProvider, string serializedSignal);
}
