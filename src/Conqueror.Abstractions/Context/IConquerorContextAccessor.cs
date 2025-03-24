// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     Provides access to the <see cref="Conqueror.ConquerorContext" />.
/// </summary>
/// <remarks>
///     This interface should be used with caution. It relies on <see cref="System.Threading.AsyncLocal{T}" /> which can have a negative performance impact on async calls.
///     It also creates a dependency on "ambient state" which can make testing more difficult.
/// </remarks>
public interface IConquerorContextAccessor
{
    /// <summary>
    ///     Gets the current <see cref="Conqueror.ConquerorContext" />. Returns <see langword="null" /> if there is no active <see cref="Conqueror.ConquerorContext" />.
    /// </summary>
    ConquerorContext? ConquerorContext { get; }

    /// <summary>
    ///     Gets the current <see cref="Conqueror.ConquerorContext" /> if there is one. Otherwise, creates a new <see cref="Conqueror.ConquerorContext" /> and returns it.
    ///     When the returned object is disposed, and it was created by this call, all contextual information is cleared. Otherwise, the disposal is a no-op.
    /// </summary>
    ConquerorContext GetOrCreate();

    /// <summary>
    ///     Clones the current <see cref="Conqueror.ConquerorContext" /> if there is one. Otherwise, creates a new <see cref="Conqueror.ConquerorContext" /> and returns it.
    ///     Cloning the context copies all downstream context data to the new context. If a context was cloned, then disposing the returned object copies
    ///     all upstream context data from the clone context to the original context. If a new context was created, all contextual information is cleared.
    /// </summary>
    ConquerorContext CloneOrCreate();
}
