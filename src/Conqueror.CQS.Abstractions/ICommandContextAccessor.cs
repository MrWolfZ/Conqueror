namespace Conqueror;

/// <summary>
///     Provides access to the current <see cref="ICommandContext" />, if one is available.
/// </summary>
/// <remarks>
///     This interface should be used with caution. It relies on <see cref="System.Threading.AsyncLocal{T}" /> which can have a negative performance impact on async calls.
///     It also creates a dependency on "ambient state" which can make testing more difficult.
/// </remarks>
public interface ICommandContextAccessor
{
    /// <summary>
    ///     Gets the current <see cref="ICommandContext" />. Returns <see langword="null" /> if there is no active <see cref="ICommandContext" />.
    /// </summary>
    ICommandContext? CommandContext { get; }

    /// <summary>
    ///     Allows setting the <see cref="ICommandContext.CommandId" /> before calling a command handler.
    ///     This method is typically called from a server-side transport implementation and does not need to be called by user-code.
    /// </summary>
    /// <param name="commandId">The ID to set for the command</param>
    void SetExternalCommandId(string commandId);
}
