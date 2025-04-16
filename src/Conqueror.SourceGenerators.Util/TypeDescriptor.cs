namespace Conqueror.SourceGenerators.Util;

public readonly record struct TypeDescriptor(
    string Name,
    string Namespace,
    string FullyQualifiedName,
    bool IsRecord,
    bool HasProperties,
    EquatableArray<PropertyDescriptor> Properties,
    EquatableArray<ParentClass> ParentClasses)
{
    public readonly string FullyQualifiedName = FullyQualifiedName;
    public readonly bool HasProperties = HasProperties;
    public readonly bool IsRecord = IsRecord;
    public readonly string Name = Name;
    public readonly string Namespace = Namespace;
    public readonly EquatableArray<ParentClass> ParentClasses = ParentClasses;
    public readonly EquatableArray<PropertyDescriptor> Properties = Properties;
}

public readonly record struct ParentClass(
    string Keyword,
    string Name)
{
    public readonly string Keyword = Keyword;
    public readonly string Name = Name;
}
