namespace Conqueror.SourceGenerators.Util;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct TupleDescriptor(EquatableArray<TypeDescriptorWrapper> Items)
{
    public readonly EquatableArray<TypeDescriptorWrapper> Items = Items;
}
