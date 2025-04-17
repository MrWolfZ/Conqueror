namespace Conqueror.SourceGenerators.Util.Messaging;

public static class TypeDescriptorExtensions
{
    public static bool IsUnitMessageResponse(this TypeDescriptor descriptor) => descriptor.FullyQualifiedName == "Conqueror.UnitMessageResponse";

    public static string FullyQualifiedName(this TypeDescriptor descriptor) => descriptor.FullyQualifiedName.Replace("<", "<global::");
}
