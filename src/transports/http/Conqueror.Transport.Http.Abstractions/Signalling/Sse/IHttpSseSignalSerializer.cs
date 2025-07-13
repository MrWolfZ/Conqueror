using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

// TODO: make this public once the API is more stable
internal interface IHttpSseSignalSerializer<TSignal>
    where TSignal : class, IHttpSseSignal<TSignal>
{
    Task<string> SerializeSignal(IServiceProvider serviceProvider, TSignal signal);

    Task<TSignal> DeserializeSignal(IServiceProvider serviceProvider, string serializedSignal);
}
