namespace Conqueror.CQS
{
    /// <summary>
    ///     Provides access to the current <see cref="CommandContext" />, if one is available.
    /// </summary>
    /// <remarks>
    ///     This interface should be used with caution. It relies on <see cref="System.Threading.AsyncLocal{T}" /> which can have a negative performance impact on async calls.
    ///     It also creates a dependency on "ambient state" which can make testing more difficult.
    /// </remarks>
    public interface ICommandContextAccessor
    {
        /// <summary>
        ///     Gets the current <see cref="CommandContext" />. Returns <see langword="null" /> if there is no active <see cref="CommandContext" />.
        /// </summary>
        CommandContext? CommandContext { get; }
    }
}
