using System.ComponentModel;

namespace Conqueror;

/// <summary>
///     Helper interface to be able to access the message and response types as generic parameters while only
///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the factory will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHttpMessageTypesInjectionFactory<out TResult>
{
    TResult Create<TMessage, TResponse>()
        where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage>;
}
