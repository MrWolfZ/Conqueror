namespace Conqueror.SourceGenerators.Util;

using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Names that are attached to incremental generator stages for tracking
/// </summary>
[SuppressMessage(
    "Major Code Smell",
    "S1118:Utility classes should not have public constructors",
    Justification = "is used via reflection"
)]
public sealed class DefaultTrackingNames
{
    public const string InitialExtraction = nameof(InitialExtraction);
    public const string RemovingNulls = nameof(RemovingNulls);
}
