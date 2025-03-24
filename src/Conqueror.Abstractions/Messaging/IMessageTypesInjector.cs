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

public interface IDefaultMessageTypesInjector : IMessageTypesInjector
{
    TResult CreateWithMessageTypes<TResult>(IDefaultMessageTypesInjectable<TResult> injectable);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DefaultMessageTypesInjector<TMessage, TResponse, TPipelineInterface, TPipelineAdapter> : IDefaultMessageTypesInjector
    where TMessage : class, IMessage<TMessage, TResponse>
    where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
    where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, TResponse>, TPipelineInterface, new()
{
    public static readonly DefaultMessageTypesInjector<TMessage, TResponse, TPipelineInterface, TPipelineAdapter> Default = new();

    public Type ConstraintType => typeof(IMessage<TMessage, TResponse>);

    /// <summary>
    ///     Helper method to be able to access the message and response types as generic parameters while only
    ///     having a generic reference to the message type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injectable that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    public TResult CreateWithMessageTypes<TResult>(IDefaultMessageTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TMessage, TResponse, TPipelineInterface, TPipelineAdapter>();
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DefaultMessageTypesInjector<TMessage, TPipelineInterface, TPipelineAdapter> : IDefaultMessageTypesInjector
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    where TPipelineInterface : class, IMessagePipeline<TMessage, UnitMessageResponse>
    where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, UnitMessageResponse>, TPipelineInterface, new()
{
    public static readonly DefaultMessageTypesInjector<TMessage, TPipelineInterface, TPipelineAdapter> Default = new();

    public Type ConstraintType => typeof(IMessage<TMessage, UnitMessageResponse>);

    /// <summary>
    ///     Helper method to be able to access the message and response types as generic parameters while only
    ///     having a generic reference to the message type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injectable that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    public TResult CreateWithMessageTypes<TResult>(IDefaultMessageTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TMessage, TPipelineInterface, TPipelineAdapter>();
}

/// <summary>
///     Helper interface to be able to access the message and response types as generic parameters while only
///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the factory will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDefaultMessageTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TMessage, TResponse, TPipelineInterface, TPipelineAdapter>()
        where TMessage : class, IMessage<TMessage, TResponse>
        where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
        where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, TResponse>, TPipelineInterface, new();

    TResult WithInjectedTypes<TMessage, TPipelineInterface, TPipelineAdapter>()
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
        where TPipelineInterface : class, IMessagePipeline<TMessage, UnitMessageResponse>
        where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, UnitMessageResponse>, TPipelineInterface, new();
}
