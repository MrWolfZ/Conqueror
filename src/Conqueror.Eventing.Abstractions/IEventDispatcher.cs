using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

/// <summary>
///     Note that this type is prefixed with <c>Conqueror</c> in order to reduce the likelihood
///     of a type name conflict with other event dispatchers.
/// </summary>
public interface IEventDispatcher
{
    Task DispatchEvent<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        where TEvent : class;

    Task DispatchEvent<TEvent>(TEvent evt, Action<IEventPipeline<TEvent>> configurePipeline, CancellationToken cancellationToken = default)
        where TEvent : class;
}
