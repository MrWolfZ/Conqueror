using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IConquerorInMemoryEventPublishingStrategy
{
    Task PublishEvent<TEvent>(IReadOnlyCollection<IEventObserver<TEvent>> eventObservers, TEvent evt, CancellationToken cancellationToken)
        where TEvent : class;
}
