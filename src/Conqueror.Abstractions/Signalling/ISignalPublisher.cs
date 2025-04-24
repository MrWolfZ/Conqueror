using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalPublisher<in TSignal>
    where TSignal : class, ISignal<TSignal>
{
    string TransportTypeName { get; }

    Task Publish(TSignal signal,
                 IServiceProvider serviceProvider,
                 ConquerorContext conquerorContext,
                 CancellationToken cancellationToken);
}
