namespace Conqueror.SourceGenerators.Iterating;

internal static class IteratorTypeSources
{
    public static (string Content, string FileName) GenerateIteratorTypeFile(IteratorTypeDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendIteratorTypeFile(in descriptor).ToString();

        _ = sb.Clear();

        var filename = sb.Append(descriptor.IteratorDescriptor.FullyQualifiedName)
            .Append("_ConquerorIteratorType.g.cs")
            .Replace("<", "__")
            .Replace(oldChar: '>', newChar: '_')
            .Replace(oldChar: ',', newChar: '_')
            .Replace(oldChar: ' ', newChar: '_')
            .ToString();

        return (content, filename);
    }

    private static StringBuilder AppendIteratorTypeFile(this StringBuilder sb, in IteratorTypeDescriptor descriptor)
    {
        var iteratorTypeDescriptor = descriptor.IteratorDescriptor;
        var itemTypeDescriptor = descriptor.ItemDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(iteratorTypeDescriptor.Namespace)
            ? null
            : sb.AppendNamespace(indentation, iteratorTypeDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, iteratorTypeDescriptor.ParentClasses);
        using (sb.AppendIteratorType(indentation, in iteratorTypeDescriptor, in itemTypeDescriptor))
        {
            _ = sb.AppendIteratorTypesProperty(indentation, in iteratorTypeDescriptor, in itemTypeDescriptor)
                .AppendCoreIteratorHandlerTypesInjectorProperty(
                    indentation,
                    in iteratorTypeDescriptor,
                    in itemTypeDescriptor
                )
                .AppendIteratorHandlerInterface(indentation, in iteratorTypeDescriptor, in itemTypeDescriptor)
                .AppendIteratorPipelineInterface(indentation, in iteratorTypeDescriptor, in itemTypeDescriptor)
                .AppendIteratorHandlerInvokeMethod(indentation, in iteratorTypeDescriptor, in itemTypeDescriptor)
                .AppendIteratorEmptyInstanceProperty(indentation, in iteratorTypeDescriptor, in itemTypeDescriptor)
                .AppendJsonSerializerContext(
                    indentation,
                    in iteratorTypeDescriptor,
                    in itemTypeDescriptor,
                    descriptor.HasJsonSerializerContext
                )
                .AppendPublicConstructorsProperty(indentation, in iteratorTypeDescriptor, in itemTypeDescriptor)
                .AppendPublicPropertiesProperty(indentation, in iteratorTypeDescriptor, in itemTypeDescriptor);
        }

        foreach (var attribute in descriptor.Attributes)
        {
            if (string.Equals(attribute.Prefix, "Core", StringComparison.Ordinal))
            {
                continue;
            }

            using (
                sb.AppendLine()
                    .AppendTransportIteratorType(
                        indentation,
                        in iteratorTypeDescriptor,
                        in itemTypeDescriptor,
                        in attribute
                    )
            )
            {
                _ = sb.AppendTransportIteratorHandlerInterface(
                        indentation,
                        in iteratorTypeDescriptor,
                        in itemTypeDescriptor,
                        in attribute
                    )
                    .AppendTransportIteratorTypesProperties(
                        indentation,
                        in iteratorTypeDescriptor,
                        in itemTypeDescriptor,
                        in attribute
                    );
            }
        }

        return sb;
    }

    private static IDisposable AppendIteratorType(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor
    )
    {
        var keyword = iteratorTypeDescriptor.IsRecord ? "record" : "class";

        return sb.AppendIndentation(indentation)
            .Append("/// <summary>")
            .AppendLineWithIndentation(indentation)
            .Append($"///     Iterator types for <see cref=\"global::{iteratorTypeDescriptor.FullyQualifiedName}\" />.")
            .AppendLineWithIndentation(indentation)
            .Append("/// </summary>")
            .AppendLineWithIndentation(indentation)
            .AppendLine(
                $"partial {keyword} {iteratorTypeDescriptor.Name} : global::Conqueror.IIterator<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>"
            )
            .AppendBlock(indentation);
    }

    private static IDisposable AppendTransportIteratorType(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor,
        in IteratorAttributeDescriptor attributeDescriptor
    )
    {
        var keyword = iteratorTypeDescriptor.IsRecord ? "record" : "class";
        var iteratorTypeName =
            attributeDescriptor.FullyQualifiedIteratorTypeName
            ?? $"{attributeDescriptor.Namespace}.I{attributeDescriptor.Prefix}Iterator";

        return sb.AppendIndentation(indentation)
            .AppendLine(
                $"partial {keyword} {iteratorTypeDescriptor.Name} : global::{iteratorTypeName}<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>"
            )
            .AppendBlock(indentation);
    }

    private static StringBuilder AppendIteratorTypesProperty(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor
    )
    {
        return sb.AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .Append("public static ")
            .AppendNewKeywordIfNecessary(iteratorTypeDescriptor)
            .AppendLine(
                $"global::Conqueror.IteratorTypes<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}, IHandler> T => new();"
            );
    }

    private static StringBuilder AppendCoreIteratorHandlerTypesInjectorProperty(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor
    )
    {
        return sb.AppendLine()
            .AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .AppendLine(
                $"static global::Conqueror.IIteratorHandlerTypesInjector global::Conqueror.IIterator<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>.CoreTypesInjector {{ get; }} = IHandler.CreateCoreTypesInjector();"
            );
    }

    private static StringBuilder AppendIteratorHandlerInterface(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor
    )
    {
        sb = sb.AppendLine()
            .AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .Append("public ")
            .AppendNewKeywordIfNecessary(iteratorTypeDescriptor)
            .AppendLine(
                $"partial interface IHandler : global::Conqueror.IIteratorHandler<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}, IHandler, IHandler.Proxy, IPipeline, IPipeline.Proxy>"
            );

        using var d = sb.AppendBlock(indentation);

        return sb.AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .AppendLine(
                $"global::System.Collections.Generic.IAsyncEnumerable<{itemTypeDescriptor.FullyQualifiedName()}> Handle({iteratorTypeDescriptor.Name} iterator, global::System.Threading.CancellationToken cancellationToken = default);"
            )
            .AppendLine()
            .AppendEditorBrowsableNeverAttribute(indentation)
            .AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .AppendLine(
                $"public sealed class Proxy : global::Conqueror.IteratorHandlerProxy<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}, IHandler>, IHandler;"
            );
    }

    private static StringBuilder AppendTransportIteratorHandlerInterface(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor,
        in IteratorAttributeDescriptor attributeDescriptor
    )
    {
        return sb.AppendIndentation(indentation)
            .AppendLine(
                $"partial interface IHandler : global::{attributeDescriptor.Namespace}.I{attributeDescriptor.Prefix}IteratorHandler<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}, IHandler>;"
            );
    }

    private static StringBuilder AppendIteratorHandlerInvokeMethod(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor
    )
    {
        return sb.AppendLine()
            .AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .AppendLine(
                $"static global::System.Collections.Generic.IAsyncEnumerable<{itemTypeDescriptor.FullyQualifiedName()}> global::Conqueror.IIterator<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>.InvokeHandler<TIHandler>(TIHandler handler, {iteratorTypeDescriptor.Name} iterator, global::System.Threading.CancellationToken cancellationToken)"
            )
            .AppendIndentation(indentation)
            .AppendSingleIndent()
            .AppendLine("=> ((IHandler)handler).Handle(iterator, cancellationToken);");
    }

    private static StringBuilder AppendIteratorPipelineInterface(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor
    )
    {
        sb = sb.AppendLine()
            .AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .Append("public ")
            .AppendNewKeywordIfNecessary(iteratorTypeDescriptor)
            .AppendLine(
                $"partial interface IPipeline : global::Conqueror.IIteratorPipeline<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>"
            );

        using var d = sb.AppendBlock(indentation);

        return sb.AppendEditorBrowsableNeverAttribute(indentation)
            .AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .AppendLine(
                $"public sealed class Proxy : global::Conqueror.IteratorPipelineProxy<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>, IPipeline;"
            );
    }

    private static StringBuilder AppendIteratorEmptyInstanceProperty(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor
    )
    {
        sb = sb.AppendLine().AppendIteratorTypeGeneratedCodeAttribute(indentation).AppendIndentation(indentation);

        if (iteratorTypeDescriptor.HasProperties() || iteratorTypeDescriptor.IsAbstract)
        {
            return sb.AppendLine(
                $"static {iteratorTypeDescriptor.Name}? global::Conqueror.IIterator<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>.EmptyInstance => null;"
            );
        }

        return sb.AppendLine(
            $"static {iteratorTypeDescriptor.Name} global::Conqueror.IIterator<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>.EmptyInstance => new();"
        );
    }

    private static StringBuilder AppendAttributeParameterProperty(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor,
        in IteratorAttributeDescriptor attributeDescriptor,
        in AttributeParameterDescriptor parameterDescriptor
    )
    {
        var iteratorTypeName =
            attributeDescriptor.FullyQualifiedIteratorTypeName
            ?? $"{attributeDescriptor.Namespace}.I{attributeDescriptor.Prefix}Iterator";

        return sb.AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .Append("static ")
            .AppendAttributeParameterPropertyType(in parameterDescriptor)
            .Append(
                $" global::{iteratorTypeName}<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>.{parameterDescriptor.Name} => "
            )
            .AppendAttributeParameterValue(in parameterDescriptor.Value)
            .AppendLine(";");
    }

    private static StringBuilder AppendJsonSerializerContext(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor,
        bool hasJsonSerializerContext
    )
    {
        if (!hasJsonSerializerContext)
        {
            return sb;
        }

        return sb.AppendLine()
            .AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .Append(
                $"static global::System.Text.Json.Serialization.JsonSerializerContext global::Conqueror.IIterator<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>.JsonSerializerContext"
            )
            .AppendLineWithIndentation(indentation)
            .AppendSingleIndent()
            .AppendLine($"=> global::{iteratorTypeDescriptor.FullyQualifiedName}JsonSerializerContext.Default;");
    }

    private static StringBuilder AppendPublicConstructorsProperty(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor
    )
    {
        return sb.AppendLine()
            .AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .AppendLine(
                $"static global::System.Collections.Generic.IEnumerable<global::System.Reflection.ConstructorInfo> global::Conqueror.IIterator<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>.PublicConstructors"
            )
            .AppendIndentation(indentation)
            .AppendSingleIndent()
            .AppendLine(
                $"=> typeof({iteratorTypeDescriptor.Name}).GetConstructors(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance);"
            );
    }

    private static StringBuilder AppendPublicPropertiesProperty(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor
    )
    {
        return sb.AppendLine()
            .AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .AppendLine(
                $"static global::System.Collections.Generic.IEnumerable<global::System.Reflection.PropertyInfo> global::Conqueror.IIterator<{iteratorTypeDescriptor.Name}, {itemTypeDescriptor.FullyQualifiedName()}>.PublicProperties"
            )
            .AppendIndentation(indentation)
            .AppendSingleIndent()
            .AppendLine(
                $"=> typeof({iteratorTypeDescriptor.Name}).GetProperties(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance);"
            );
    }

    private static StringBuilder AppendTransportIteratorTypesProperties(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor iteratorTypeDescriptor,
        in TypeDescriptor itemTypeDescriptor,
        in IteratorAttributeDescriptor attributeDescriptor
    )
    {
        foreach (var property in attributeDescriptor.Properties)
        {
            _ = sb.AppendAttributeParameterProperty(
                indentation,
                in iteratorTypeDescriptor,
                in itemTypeDescriptor,
                in attributeDescriptor,
                in property
            );
        }

        return sb;
    }

    private static StringBuilder AppendNewKeywordIfNecessary(
        this StringBuilder sb,
        in TypeDescriptor iteratorTypeDescriptor
    )
    {
        return iteratorTypeDescriptor.BaseTypes.Any(t =>
            t.Attributes.Any(a =>
                a.Attributes.Any(bt =>
                    string.Equals(
                        bt.FullyQualifiedName,
                        "Conqueror.Iterating.IteratorTransportAttribute",
                        StringComparison.Ordinal
                    )
                )
            )
        )
            ? sb.Append("new ")
            : sb;
    }

    private static StringBuilder AppendAttributeParameterPropertyType(
        this StringBuilder sb,
        in AttributeParameterDescriptor parameterDescriptor
    )
    {
        if (parameterDescriptor is { IsPrimitive: false, IsArray: false })
        {
            _ = sb.Append("global::");
        }

        _ = sb.Append(parameterDescriptor.FullyQualifiedTypeName);

        if (parameterDescriptor.Value.IsNull)
        {
            _ = sb.Append(value: '?');
        }

        return sb;
    }

    private static StringBuilder AppendAttributeParameterValue(
        this StringBuilder sb,
        in AttributeParameterValueDescriptor valueDescriptor
    )
    {
        if (valueDescriptor.IsNull)
        {
            return sb.Append("null");
        }

        if (valueDescriptor.Value is string)
        {
            return sb.Append($"\"{valueDescriptor.Value}\"");
        }

        if (valueDescriptor.Values is not null)
        {
            _ = sb.Append("new[] { ");

            for (var i = 0; i < valueDescriptor.Values.Value.Count; i += 1)
            {
                _ = sb.AppendAttributeParameterValue(valueDescriptor.Values.Value[i]);

                if (i < valueDescriptor.Values.Value.Count - 1)
                {
                    _ = sb.Append(value: ',');
                }

                _ = sb.Append(value: ' ');
            }

            return sb.Append('}');
        }

        return sb.Append(valueDescriptor.Value);
    }

    private static StringBuilder AppendIteratorTypeGeneratedCodeAttribute(
        this StringBuilder sb,
        Indentation indentation
    )
    {
        var version = typeof(IteratorTypeSources).Assembly.GetName().Version.ToString();

        return sb.AppendGeneratedCodeAttribute(indentation, typeof(IteratorTypeGenerator).FullName ?? "", version);
    }
}
