using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     Base interface for transports to be able to get an injector that works
///     with their specific constraint interface.
/// </summary>
public interface IEventNotificationTypesInjector
{
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

    internal IEventNotificationTypesInjector WithHandlerType<THandler>()
        where THandler : class, IGeneratedEventNotificationHandler;
}

public interface IDefaultEventNotificationTypesInjector : IEventNotificationTypesInjector
{
    TResult CreateWithEventNotificationTypes<TResult>(IDefaultEventNotificationTypesInjectable<TResult> injectable);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DefaultEventNotificationTypesInjector<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    TEventNotification,
    TGeneratedHandlerInterface,
    TGeneratedHandlerAdapter>
    : IDefaultEventNotificationTypesInjector
    where TEventNotification : class, IEventNotification<TEventNotification>
    where TGeneratedHandlerInterface : class, IGeneratedEventNotificationHandler<TEventNotification, TGeneratedHandlerInterface>
    where TGeneratedHandlerAdapter : GeneratedEventNotificationHandlerAdapter<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>, TGeneratedHandlerInterface, new()
{
    public static readonly DefaultEventNotificationTypesInjector<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter> Default = new();

    public Type EventNotificationType => typeof(TEventNotification);

    /// <summary>
    ///     This property and the <see cref="FakeNotification" /> ensure that the native code for
    ///     <see cref="WithHandlerType{THandler}" /> gets emitted correctly when running in AOT mode.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "necessary for AOT compatibility")]
    private static DefaultEventNotificationTypesInjector<FakeNotification, FakeNotification.IHandler, FakeNotification.IHandler.Adapter>.WithHandlerType<FakeNotificationHandler> Fake { get; } = new();

    /// <summary>
    ///     Helper method to be able to access the event notification types as generic parameters while only
    ///     having a generic reference to the event notification type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injectable that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    public TResult CreateWithEventNotificationTypes<TResult>(IDefaultEventNotificationTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>();

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                  Justification = "all types are statically known, so the necessary native code should be present")]
    IEventNotificationTypesInjector IEventNotificationTypesInjector.WithHandlerType<THandler>()
    {
        Debug.Assert(typeof(THandler).IsAssignableTo(typeof(TGeneratedHandlerInterface)), $"expected handler type '{typeof(THandler)}' to be assignable to '{typeof(TGeneratedHandlerInterface)}'");

        return Activator.CreateInstance(typeof(WithHandlerType<>)
                                            .MakeGenericType(typeof(TEventNotification), typeof(TGeneratedHandlerInterface), typeof(TGeneratedHandlerAdapter), typeof(THandler)))
                   as IEventNotificationTypesInjector
               ?? throw new InvalidOperationException("cannot create instance of WithHandlerType<THandler>");
    }

    private sealed class WithHandlerType<THandler> : IDefaultEventNotificationTypesInjector
        where THandler : class, TGeneratedHandlerInterface
    {
        public TResult CreateWithEventNotificationTypes<TResult>(IDefaultEventNotificationTypesInjectable<TResult> injectable)
            => injectable.WithInjectedTypes<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter, THandler>();

        IEventNotificationTypesInjector IEventNotificationTypesInjector.WithHandlerType<T>()
            => throw new NotSupportedException("cannot set handler type multiple times for types injector");
    }

    private sealed class FakeNotification : IEventNotification<FakeNotification>
    {
        public static IDefaultEventNotificationTypesInjector DefaultTypeInjector => throw new NotSupportedException();

        public static IReadOnlyCollection<IEventNotificationTypesInjector> TypeInjectors => throw new NotSupportedException();

        public static EventNotificationTypes<FakeNotification, IHandler> T => throw new NotSupportedException();

        public static FakeNotification EmptyInstance => throw new NotSupportedException();

        public interface IHandler : IGeneratedEventNotificationHandler<FakeNotification, IHandler>
        {
            static Task IGeneratedEventNotificationHandler<FakeNotification, IHandler>
                .Invoke(IHandler handler, FakeNotification notification, CancellationToken cancellationToken)
                => throw new NotSupportedException();

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedEventNotificationHandlerAdapter<FakeNotification, IHandler, Adapter>, IHandler;
        }
    }

    private sealed class FakeNotificationHandler : FakeNotification.IHandler;
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TEventNotification,
        TGeneratedHandlerInterface,
        TGeneratedHandlerAdapter>()
        where TEventNotification : class, IEventNotification<TEventNotification>
        where TGeneratedHandlerInterface : class, IGeneratedEventNotificationHandler<TEventNotification, TGeneratedHandlerInterface>
        where TGeneratedHandlerAdapter : GeneratedEventNotificationHandlerAdapter<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>, TGeneratedHandlerInterface, new()
    {
        Debug.Assert(false, "this method should never be called");
        throw new NotSupportedException("did you mean to call WithInjectedTypes<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter, THandler>?");
    }

    TResult WithInjectedTypes<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TEventNotification,
        TGeneratedHandlerInterface,
        TGeneratedHandlerAdapter,
        THandler>()
        where TEventNotification : class, IEventNotification<TEventNotification>
        where TGeneratedHandlerInterface : class, IGeneratedEventNotificationHandler<TEventNotification, TGeneratedHandlerInterface>
        where TGeneratedHandlerAdapter : GeneratedEventNotificationHandlerAdapter<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>, TGeneratedHandlerInterface, new()
        where THandler : class, TGeneratedHandlerInterface
    {
        Debug.Assert(false, "this method should never be called");
        throw new NotSupportedException("did you mean to call WithInjectedTypes<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>?");
    }
}
