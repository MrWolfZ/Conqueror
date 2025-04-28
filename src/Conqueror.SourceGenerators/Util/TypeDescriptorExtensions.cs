using System.Linq;

namespace Conqueror.SourceGenerators.Util;

public static class TypeDescriptorExtensions
{
    public static bool IsUnitMessageResponse(this TypeDescriptor descriptor) => descriptor.FullyQualifiedName == "Conqueror.UnitMessageResponse";

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

        if (descriptor.TypeArguments.Count == 0)
        {
            return $"global::{descriptor.FullyQualifiedName}";
        }

        return $"global::{descriptor.FullyQualifiedName.Substring(0, descriptor.FullyQualifiedName.IndexOf('<'))}<{string.Join(", ", descriptor.TypeArguments.Select(i => i.Descriptor.FullyQualifiedName()))}>";
    }

    public static bool HasProperties(this TypeDescriptor descriptor) => descriptor.Properties.Count > 0 || descriptor.BaseTypes.Any(t => t.Properties.Count > 0);
}
