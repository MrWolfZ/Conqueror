using System.Linq;

namespace Conqueror.SourceGenerators.Util;

public static class TypeDescriptorExtensions
{
    public static bool IsUnitMessageResponse(this TypeDescriptor descriptor) => descriptor.FullyQualifiedName == "Conqueror.UnitMessageResponse";

    public static string FullyQualifiedName(this TypeDescriptor descriptor) => descriptor.FullyQualifiedName.Replace("<", "<global::");

    public static bool HasProperties(this TypeDescriptor descriptor) => descriptor.Properties.Count > 0 || descriptor.BaseTypes.Any(t => t.Properties.Count > 0);
}
