using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     Base interface for transports to be able to get an injector that works
///     with their specific constraint interface.
/// </summary>
public interface IEventNotificationTypesInjector
{
    public Type EventNotificationType { get; }

    static IReadOnlyCollection<IEventNotificationTypesInjector> GetTypeInjectorsForEventNotificationType<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TEventNotification>()
    {
        return typeof(TEventNotification).GetProperties(BindingFlags.NonPublic | BindingFlags.Static)
                                         .Where(p => p.PropertyType.IsAssignableTo(typeof(IEventNotificationTypesInjector)))
                                         .Select(p => p.GetValue(null))
                                         .OfType<IEventNotificationTypesInjector>()
                                         .ToList();
    }
}

public interface IDefaultEventNotificationTypesInjector : IEventNotificationTypesInjector
{
    TResult CreateWithEventNotificationTypes<TResult>(IDefaultEventNotificationTypesInjectable<TResult> injectable);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DefaultEventNotificationTypesInjector<
    TEventNotification,
    TGeneratedHandlerInterface,
    TGeneratedHandlerAdapter>
    : IDefaultEventNotificationTypesInjector
    where TEventNotification : class, IEventNotification<TEventNotification>
    where TGeneratedHandlerInterface : class, IGeneratedEventNotificationHandler<TEventNotification>
    where TGeneratedHandlerAdapter : GeneratedEventNotificationHandlerAdapter<TEventNotification>, TGeneratedHandlerInterface, new()
{
    public static readonly DefaultEventNotificationTypesInjector<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter> Default = new();

    public Type EventNotificationType => typeof(TEventNotification);

    /// <summary>
    ///     Helper method to be able to access the event notification types as generic parameters while only
    ///     having a generic reference to the event notification type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injectable that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    public TResult CreateWithEventNotificationTypes<TResult>(IDefaultEventNotificationTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>();
}

/// <summary>
///     Helper interface to be able to access the event notification types as generic parameters while only
///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the factory will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDefaultEventNotificationTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<
        TEventNotification,
        TGeneratedHandlerInterface,
        TGeneratedHandlerAdapter>()
        where TEventNotification : class, IEventNotification<TEventNotification>
        where TGeneratedHandlerInterface : class, IGeneratedEventNotificationHandler<TEventNotification>
        where TGeneratedHandlerAdapter : GeneratedEventNotificationHandlerAdapter<TEventNotification>, TGeneratedHandlerInterface, new();
}
