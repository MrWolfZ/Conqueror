using System;
using System.Collections.Generic;

namespace Conqueror;

public interface ICommandTransportRegistry
{
    IReadOnlyCollection<(Type CommandType, Type? ResponseType, TTransportMarkerAttribute Attribute)> GetCommandTypesForTransport<TTransportMarkerAttribute>()
        where TTransportMarkerAttribute : Attribute;
}
