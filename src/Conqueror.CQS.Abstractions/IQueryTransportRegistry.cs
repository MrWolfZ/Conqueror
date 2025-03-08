using System;
using System.Collections.Generic;

namespace Conqueror;

public interface IQueryTransportRegistry
{
    IReadOnlyCollection<(Type QueryType, Type ResponseType, TTransportMarkerAttribute Attribute)> GetQueryTypesForTransport<TTransportMarkerAttribute>()
        where TTransportMarkerAttribute : Attribute;
}
