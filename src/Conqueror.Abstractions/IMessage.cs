using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Conqueror;

[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "The parameter is used for type checking")]
public interface IMessage<TResponse> : IMessageWithTypesInjectionFactory;

public interface IMessage : IMessage<UnitMessageResponse>;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMessageWithTypesInjectionFactory
{
    /// <summary>
    ///     Helper method to be able to access the message and response types as generic parameters while only
    ///     having a generic reference to the message type. This allows bypassing reflection.
    /// </summary>
    /// <param name="factory">The factory that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract TResult CreateWithMessageTypes<TResult>(IMessageTypesInjectionFactory<TResult> factory);
}
