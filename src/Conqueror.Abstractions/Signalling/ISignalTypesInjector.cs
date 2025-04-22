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
public interface ISignalTypesInjector
{
    static IReadOnlyCollection<ISignalTypesInjector> GetTypeInjectorsForSignalType<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TSignal>()
    {
        return typeof(TSignal).GetProperties(BindingFlags.NonPublic | BindingFlags.Static)
                              .Where(p => p.PropertyType.IsAssignableTo(typeof(ISignalTypesInjector)))
                              .Select(p => p.GetValue(null))
                              .OfType<ISignalTypesInjector>()
                              .ToList();
    }

    ISignalTypesInjector WithHandlerType<THandler>()
        where THandler : class, IGeneratedSignalHandler;
}

public interface IDefaultSignalTypesInjector : ISignalTypesInjector
{
    TResult CreateWithSignalTypes<TResult>(IDefaultSignalTypesInjectable<TResult> injectable);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DefaultSignalTypesInjector<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    TSignal,
    TGeneratedHandlerInterface,
    TGeneratedHandlerAdapter>
    : IDefaultSignalTypesInjector
    where TSignal : class, ISignal<TSignal>
    where TGeneratedHandlerInterface : class, IGeneratedSignalHandler<TSignal, TGeneratedHandlerInterface>
    where TGeneratedHandlerAdapter : GeneratedSignalHandlerAdapter<TSignal, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>, TGeneratedHandlerInterface, new()
{
    public static readonly DefaultSignalTypesInjector<TSignal, TGeneratedHandlerInterface, TGeneratedHandlerAdapter> Default = new();

    public Type SignalType => typeof(TSignal);

    /// <summary>
    ///     This property and the <see cref="FakeSignal" /> ensure that the native code for
    ///     <see cref="WithHandlerType{THandler}" /> gets emitted correctly when running in AOT mode.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "necessary for AOT compatibility")]
    private static DefaultSignalTypesInjector<FakeSignal, FakeSignal.IHandler, FakeSignal.IHandler.Adapter>.WithHandlerType<FakeSignalHandler> Fake { get; } = new();

    /// <summary>
    ///     Helper method to be able to access the signal types as generic parameters while only
    ///     having a generic reference to the signal type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injectable that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    public TResult CreateWithSignalTypes<TResult>(IDefaultSignalTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TSignal, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>();

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                  Justification = "all types are statically known, so the necessary native code should be present")]
    ISignalTypesInjector ISignalTypesInjector.WithHandlerType<THandler>()
    {
        Debug.Assert(typeof(THandler).IsAssignableTo(typeof(TGeneratedHandlerInterface)), $"expected handler type '{typeof(THandler)}' to be assignable to '{typeof(TGeneratedHandlerInterface)}'");

        return Activator.CreateInstance(typeof(WithHandlerType<>)
                                            .MakeGenericType(typeof(TSignal), typeof(TGeneratedHandlerInterface), typeof(TGeneratedHandlerAdapter), typeof(THandler)))
                   as ISignalTypesInjector
               ?? throw new InvalidOperationException("cannot create instance of WithHandlerType<THandler>");
    }

    private sealed class WithHandlerType<THandler> : IDefaultSignalTypesInjector
        where THandler : class, TGeneratedHandlerInterface
    {
        public TResult CreateWithSignalTypes<TResult>(IDefaultSignalTypesInjectable<TResult> injectable)
            => injectable.WithInjectedTypes<TSignal, TGeneratedHandlerInterface, TGeneratedHandlerAdapter, THandler>();

        ISignalTypesInjector ISignalTypesInjector.WithHandlerType<T>()
            => throw new NotSupportedException("cannot set handler type multiple times for types injector");
    }

    private sealed class FakeSignal : ISignal<FakeSignal>
    {
        public static IDefaultSignalTypesInjector DefaultTypeInjector => throw new NotSupportedException();

        public static IReadOnlyCollection<ISignalTypesInjector> TypeInjectors => throw new NotSupportedException();

        public static SignalTypes<FakeSignal, IHandler> T => throw new NotSupportedException();

        public static FakeSignal EmptyInstance => throw new NotSupportedException();

        public interface IHandler : IGeneratedSignalHandler<FakeSignal, IHandler>
        {
            static Task IGeneratedSignalHandler<FakeSignal, IHandler>
                .Invoke(IHandler handler, FakeSignal signal, CancellationToken cancellationToken)
                => throw new NotSupportedException();

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedSignalHandlerAdapter<FakeSignal, IHandler, Adapter>, IHandler;
        }
    }

    private sealed class FakeSignalHandler : FakeSignal.IHandler;
}

/// <summary>
///     Helper interface to be able to access the signal types as generic parameters while only
///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the factory will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDefaultSignalTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        TGeneratedHandlerInterface,
        TGeneratedHandlerAdapter>()
        where TSignal : class, ISignal<TSignal>
        where TGeneratedHandlerInterface : class, IGeneratedSignalHandler<TSignal, TGeneratedHandlerInterface>
        where TGeneratedHandlerAdapter : GeneratedSignalHandlerAdapter<TSignal, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>, TGeneratedHandlerInterface, new()
    {
        Debug.Assert(false, "this method should never be called");
        throw new NotSupportedException("did you mean to call WithInjectedTypes<TSignal, TGeneratedHandlerInterface, TGeneratedHandlerAdapter, THandler>?");
    }

    TResult WithInjectedTypes<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        TGeneratedHandlerInterface,
        TGeneratedHandlerAdapter,
        THandler>()
        where TSignal : class, ISignal<TSignal>
        where TGeneratedHandlerInterface : class, IGeneratedSignalHandler<TSignal, TGeneratedHandlerInterface>
        where TGeneratedHandlerAdapter : GeneratedSignalHandlerAdapter<TSignal, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>, TGeneratedHandlerInterface, new()
        where THandler : class, TGeneratedHandlerInterface
    {
        Debug.Assert(false, "this method should never be called");
        throw new NotSupportedException("did you mean to call WithInjectedTypes<TSignal, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>?");
    }
}
