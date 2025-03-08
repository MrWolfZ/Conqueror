using System;
using System.Collections.Generic;

namespace Conqueror;

public interface IEventTransportRegistry
{
    IReadOnlyCollection<(Type EventType, TTransportMarkerAttribute Attribute)> GetEventTypesForReceiver<TTransportMarkerAttribute>()
        where TTransportMarkerAttribute : ConquerorEventTransportAttribute;
}
