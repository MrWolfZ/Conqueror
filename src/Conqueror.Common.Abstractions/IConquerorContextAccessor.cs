namespace Conqueror
{
    /// <summary>
    ///     Provides access to the <see cref="IConquerorContext" />.
    /// </summary>
    /// <remarks>
    ///     This interface should be used with caution. It relies on <see cref="System.Threading.AsyncLocal{T}" /> which can have a negative performance impact on async calls.
    ///     It also creates a dependency on "ambient state" which can make testing more difficult.
    /// </remarks>
    public interface IConquerorContextAccessor
    {
        /// <summary>
        ///     Gets the current <see cref="IConquerorContext" />. Returns <see langword="null" /> if there is no active <see cref="IConquerorContext" />.
        /// </summary>
        IConquerorContext? ConquerorContext { get; }

        /// <summary>
        ///     Gets the current <see cref="IConquerorContext" /> if it is active. Otherwise creates a new <see cref="IConquerorContext" /> and returns it.
        ///     When the returned object is disposed and it was created by this call, all contextual information is cleared. Otherwise the disposal is a no-op.
        /// </summary>
        IDisposableConquerorContext GetOrCreate();
    }
}
