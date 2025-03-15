using System;
using System.Collections.Generic;

namespace Conqueror;

public interface IMessageTransportRegistry
{
    // TODO: remove
    IReadOnlyCollection<(Type MessageType, Type ResponseType, TAttribute Attribute)> GetMessageTypesForTransport<TAttribute>()
        where TAttribute : Attribute;

    IReadOnlyCollection<(Type MessageType, Type ResponseType, IMessageTypesInjector? TypeInjector)> GetMessageTypesForTransportInterface<TInterface>()
        where TInterface : class;
}
