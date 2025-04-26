using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class SignalHandlerTypeServiceRegistry
{
    private static readonly List<Action<ISignalHandlerServiceRegisterable>> RegistrationActions = [];

    public static void RegisterHandlerType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
        where THandler : class, ISignalHandler
        => RegisterHandlerTypeInternal<THandler>();

    internal static void RunWithRegisteredTypes(ISignalHandlerServiceRegisterable registerable)
    {
        foreach (var action in RegistrationActions)
        {
            action(registerable);
        }
    }

    private static void RegisterHandlerTypeInternal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
        where THandler : class, ISignalHandler
    {
        RegistrationActions.Add(r => r.Register<THandler>());
    }
}

internal interface ISignalHandlerServiceRegisterable
{
    void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
        where THandler : class, ISignalHandler;
}
