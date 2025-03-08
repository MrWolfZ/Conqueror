using System;
using System.Collections.Generic;

namespace Conqueror;

public interface IEventTransportRegistry
{
    IReadOnlyCollection<(Type EventType, TAttribute Attribute)> GetEventTypesForReceiver<TAttribute>()
        where TAttribute : EventTransportAttribute;
}
