using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Conqueror;

/// <summary>
///     Base interface for transports to be able to get an injector that works
///     with their specific constraint interface.
/// </summary>
public interface IMessageTypesInjector
{
    Type ConstraintType { get; }

    static IReadOnlyCollection<IMessageTypesInjector> GetTypeInjectorsForMessageType<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TMessage>()
    {
        return typeof(TMessage).GetProperties(BindingFlags.NonPublic | BindingFlags.Static)
                               .Where(p => p.PropertyType.IsAssignableTo(typeof(IMessageTypesInjector)))
                               .Select(p => p.GetValue(null))
                               .OfType<IMessageTypesInjector>()
                               .ToList();
    }
}
