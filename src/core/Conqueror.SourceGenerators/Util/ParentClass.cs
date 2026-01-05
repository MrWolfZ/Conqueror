namespace Conqueror.SourceGenerators.Util;

using Microsoft.CodeAnalysis;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct ParentClass(string Keyword, string Name, Accessibility Accessibility)
{
    public readonly Accessibility Accessibility = Accessibility;
    public readonly string Keyword = Keyword;
    public readonly string Name = Name;
}
