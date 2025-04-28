namespace Conqueror.SourceGenerators.Util;

public readonly record struct TupleDescriptor(
    EquatableArray<TypeDescriptorWrapper> Items)
{
    public readonly EquatableArray<TypeDescriptorWrapper> Items = Items;
}
