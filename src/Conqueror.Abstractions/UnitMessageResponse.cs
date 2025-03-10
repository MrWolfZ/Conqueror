namespace Conqueror;

/// <summary>
///     Placeholder response type for messages which don't have a response.
/// </summary>
public sealed record UnitMessageResponse
{
    public static readonly UnitMessageResponse Instance = new();
}
