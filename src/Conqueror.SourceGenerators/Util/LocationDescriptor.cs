using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Conqueror.SourceGenerators.Util;

public readonly record struct LocationDescriptor(
    string FilePath,
    TextSpan TextSpan,
    LinePositionSpan LineSpan)
{
    public readonly string FilePath = FilePath;
    public readonly LinePositionSpan LineSpan = LineSpan;
    public readonly TextSpan TextSpan = TextSpan;

    public Location ToLocation()
        => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationDescriptor? CreateFrom(SyntaxToken? token)
        => CreateFrom(token?.GetLocation());

    private static LocationDescriptor? CreateFrom(Location? location)
    {
        if (location?.SourceTree is null)
        {
            return null;
        }

        return new LocationDescriptor(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }
}
