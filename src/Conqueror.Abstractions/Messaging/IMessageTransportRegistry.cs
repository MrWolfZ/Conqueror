using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageTransportRegistry
{
    IReadOnlyCollection<(Type MessageType, Type ResponseType, IMessageTypesInjector? TypeInjector)> GetMessageTypesForTransportInterface<TInterface>()
        where TInterface : class;
}
