namespace Conqueror.SourceGenerators;

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
    ParentClass? ParentClass)
{
    public readonly string FullyQualifiedName = FullyQualifiedName;
    public readonly bool IsRecord = IsRecord;
    public readonly string Name = Name;
    public readonly string Namespace = Namespace;
    public readonly ParentClass? ParentClass = ParentClass;
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
