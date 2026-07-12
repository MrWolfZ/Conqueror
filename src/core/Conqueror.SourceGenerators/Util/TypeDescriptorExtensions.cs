namespace Conqueror.SourceGenerators.Util;

internal static class TypeDescriptorExtensions
{
    public static bool IsUnitMessageResponse(this TypeDescriptor descriptor) =>
        string.Equals(descriptor.FullyQualifiedName, "Conqueror.UnitMessageResponse", StringComparison.Ordinal);

    public static string FullyQualifiedName(this TypeDescriptor descriptor)
    {
        if (descriptor.IsPrimitive)
        {
            return descriptor.FullyQualifiedName;
        }

        if (descriptor.Tuple is { } t)
        {
            return $"({string.Join(", ", t.Items.Select(i => i.Descriptor.FullyQualifiedName()))})";
        }

        if (descriptor.Enumerable is { IsArray: true })
        {
            return $"{descriptor.Enumerable.Value.ItemType.Descriptor.FullyQualifiedName()}[]";
        }

        if (descriptor.TypeArguments.Count is 0)
        {
            return $"global::{descriptor.FullyQualifiedName}";
        }

        return
            $"global::{descriptor.FullyQualifiedName.Substring(startIndex: 0, descriptor.FullyQualifiedName.IndexOf('<'))}<{string.Join(", ", descriptor.TypeArguments.Select(i => i.Descriptor.FullyQualifiedName()))}>";
    }

    public static bool HasProperties(this TypeDescriptor descriptor) =>
        descriptor.Properties.Count > 0 || descriptor.BaseTypes.Any(t => t.Properties.Count > 0);
}
