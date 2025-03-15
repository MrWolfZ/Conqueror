namespace Conqueror.SourceGenerators.Messaging;

public readonly record struct MessageTypeToGenerate(
    TypeDescriptor MessageTypeDescriptor,
    TypeDescriptor? ResponseTypeDescriptor)
{
    public readonly TypeDescriptor MessageTypeDescriptor = MessageTypeDescriptor;
    public readonly TypeDescriptor? ResponseTypeDescriptor = ResponseTypeDescriptor;
}

public readonly record struct TypeDescriptor(
    string Name,
    string Namespace,
    string FullyQualifiedName,
    bool IsRecord,
    bool HasProperties,
    EquatableArray<PropertyDescriptor> Properties,
    ParentClass? ParentClass)
{
    public readonly string FullyQualifiedName = FullyQualifiedName;
    public readonly bool HasProperties = HasProperties;
    public readonly bool IsRecord = IsRecord;
    public readonly string Name = Name;
    public readonly string Namespace = Namespace;
    public readonly ParentClass? ParentClass = ParentClass;
    public readonly EquatableArray<PropertyDescriptor> Properties = Properties;
}

public readonly record struct PropertyDescriptor(
    string Name,
    string FullyQualifiedTypeName,
    bool IsPrimitive,
    bool IsNullable,
    bool IsString,
    EnumerableDescriptor? Enumerable)
{
    public readonly EnumerableDescriptor? Enumerable = Enumerable;
    public readonly string FullyQualifiedTypeName = FullyQualifiedTypeName;
    public readonly bool IsNullable = IsNullable;
    public readonly bool IsPrimitive = IsPrimitive;
    public readonly bool IsString = IsString;
    public readonly string Name = Name;
}

public readonly record struct EnumerableDescriptor(
    string FullyQualifiedTypeName,
    string FullyQualifiedItemTypeName,
    bool IsArray,
    bool IsPrimitive,
    bool IsNullable)
{
    public readonly string FullyQualifiedItemTypeName = FullyQualifiedItemTypeName;
    public readonly string FullyQualifiedTypeName = FullyQualifiedTypeName;
    public readonly bool IsArray = IsArray;
    public readonly bool IsNullable = IsNullable;
    public readonly bool IsPrimitive = IsPrimitive;
}

public sealed record ParentClass(
    string Keyword,
    string Name,
    string Constraints,
    ParentClass? Child)
{
    public ParentClass? Child { get; } = Child;
    public string Constraints { get; } = Constraints;
    public string Keyword { get; } = Keyword;
    public string Name { get; } = Name;
}
