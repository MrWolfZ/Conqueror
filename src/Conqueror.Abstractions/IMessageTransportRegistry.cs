using System;
using System.Collections.Generic;

namespace Conqueror;

public interface IMessageTransportRegistry
{
    IReadOnlyCollection<(Type MessageType, Type ResponseType, TAttribute Attribute)> GetMessageTypesForTransport<TAttribute>()
        where TAttribute : Attribute;
}
