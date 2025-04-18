using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageTransportRegistry
{
    IReadOnlyCollection<(Type MessageType, Type ResponseType, TTypesInjector TypesInjector)> GetMessageTypesForTransport<TTypesInjector>()
        where TTypesInjector : class, IMessageTypesInjector;
}
