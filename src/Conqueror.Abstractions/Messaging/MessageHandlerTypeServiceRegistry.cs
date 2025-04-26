using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class MessageHandlerTypeServiceRegistry
{
    private static readonly List<Action<IMessageHandlerServiceRegisterable>> RegistrationActions = [];

    public static void RegisterHandlerType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
        where THandler : class, IMessageHandler
        => RegisterHandlerTypeInternal<THandler>();

    internal static void RunWithRegisteredTypes(IMessageHandlerServiceRegisterable registerable)
    {
        foreach (var action in RegistrationActions)
        {
            action(registerable);
        }
    }

    private static void RegisterHandlerTypeInternal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
        where THandler : class, IMessageHandler
    {
        RegistrationActions.Add(r => r.Register<THandler>());
    }
}

internal interface IMessageHandlerServiceRegisterable
{
    void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
        where THandler : class, IMessageHandler;
}
