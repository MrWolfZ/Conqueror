namespace Conqueror;

/// <summary>
///     Placeholder response type for commands which don't have a response.
/// </summary>
public sealed record UnitCommandResponse
{
    public static readonly UnitCommandResponse Instance = new();
}
