namespace Conqueror.SourceGenerators.Iterating;

using Microsoft.CodeAnalysis;

internal static class IteratorHandlerTypeSources
{
    public static (string Content, string FileName) GenerateIteratorHandlerType(
        IteratorHandlerTypeDescriptor descriptor
    )
    {
        var sb = new StringBuilder();

        var content = sb.AppendHandlerTypeFile(in descriptor).ToString();

        _ = sb.Clear();

        var filename = sb.Append(descriptor.HandlerDescriptor.FullyQualifiedName)
            .Append("_ConquerorIteratorHandlerType.g.cs")
            .Replace("<", "__")
            .Replace(oldChar: '>', newChar: '_')
            .Replace(oldChar: ',', newChar: '_')
            .Replace(oldChar: ' ', newChar: '_')
            .ToString();

        return (content, filename);
    }

    private static StringBuilder AppendHandlerTypeFile(
        this StringBuilder sb,
        in IteratorHandlerTypeDescriptor descriptor
    )
    {
        var handlerDescriptor = descriptor.HandlerDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(handlerDescriptor.Namespace)
            ? null
            : sb.AppendNamespace(indentation, handlerDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, handlerDescriptor.ParentClasses);
        using var mt = sb.AppendIteratorHandlerType(indentation, in handlerDescriptor, in descriptor.IteratorTypes);

        return sb.AppendGetTypeInjectorsMethod(indentation, in handlerDescriptor, in descriptor.IteratorTypes)
            .AppendModuleInitializerMethod(indentation, in handlerDescriptor);
    }

    private static IDisposable AppendIteratorHandlerType(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor handlerDescriptor,
        in EquatableArray<IteratorTypeDescriptor> iteratorDescriptors
    )
    {
        var keyword = handlerDescriptor.IsRecord ? "record" : "class";
        _ = sb.AppendIndentation(indentation)
            .Append(
                $"partial {keyword} {handlerDescriptor.Name} : global::Conqueror.IIteratorHandlerWithSourceGeneration"
            );

        foreach (
            var (ns, prefix) in iteratorDescriptors
                .OrderBy(d => d.IteratorDescriptor.FullyQualifiedName, StringComparer.OrdinalIgnoreCase)
                .SelectMany(d => d.Attributes, (d, a) => new { d, a })
                .Where(t => !string.Equals(t.a.Prefix, "Core", StringComparison.Ordinal))
                .Select(t => (t.a.Namespace, t.a.Prefix))
                .Distinct()
        )
        {
            _ = sb.Append($", global::{ns}.I{prefix}IteratorHandler");
        }

        return sb.AppendLine().AppendBlock(indentation);
    }

    private static StringBuilder AppendGetTypeInjectorsMethod(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor handlerDescriptor,
        in EquatableArray<IteratorTypeDescriptor> iteratorDescriptors
    )
    {
        if (
            handlerDescriptor.Methods.Any(m =>
                m.Name.Equals("GetTypeInjectors", StringComparison.Ordinal)
                || m.Name.Equals("Conqueror.IIteratorHandler.GetTypeInjectors", StringComparison.Ordinal)
            )
        )
        {
            return sb;
        }

        _ = sb.AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .AppendLine(
                "static global::System.Collections.Generic.IEnumerable<global::Conqueror.IIteratorHandlerTypesInjector> global::Conqueror.IIteratorHandler.GetTypeInjectors()"
            );

        using var b = sb.AppendBlock(indentation);

        string? currentIteratorName = null;

        foreach (
            var d in iteratorDescriptors.OrderBy(
                d => d.IteratorDescriptor.FullyQualifiedName,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            if (!string.Equals(currentIteratorName, d.IteratorDescriptor.FullyQualifiedName, StringComparison.Ordinal))
            {
                if (currentIteratorName is not null)
                {
                    _ = sb.AppendLine();
                }

                _ = sb.AppendIndentation(indentation)
                    .AppendLine(
                        $"yield return global::{d.IteratorDescriptor.FullyQualifiedName}.IHandler.CreateCoreTypesInjector<{handlerDescriptor.Name}>();"
                    );
            }

            currentIteratorName = d.IteratorDescriptor.FullyQualifiedName;

            foreach (
                var prefix in d
                    .Attributes.Where(a => !string.Equals(a.Prefix, "Core", StringComparison.Ordinal))
                    .OrderBy(a => a.Prefix, StringComparer.OrdinalIgnoreCase)
                    .Select(a => a.Prefix)
            )
            {
                _ = sb.AppendIndentation(indentation)
                    .AppendLine(
                        $"yield return global::{d.IteratorDescriptor.FullyQualifiedName}.IHandler.Create{prefix}TypesInjector<{handlerDescriptor.Name}>();"
                    );
            }
        }

        return sb;
    }

    private static StringBuilder AppendModuleInitializerMethod(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor handlerDescriptor
    )
    {
        if (
            handlerDescriptor.Methods.Any(m => m.Name.Equals("ModuleInitializer", StringComparison.Ordinal))
            || handlerDescriptor.Accessibility is not (Accessibility.Public or Accessibility.Internal)
            || handlerDescriptor.ParentClasses.Any(pc =>
                pc.Accessibility is not (Accessibility.Public or Accessibility.Internal)
            )
            || handlerDescriptor.IsAbstract
            || handlerDescriptor.TypeArguments.Count > 0
        )
        {
            return sb;
        }

        return sb.AppendLine()
            .AppendIteratorTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .Append("[global::System.Runtime.CompilerServices.ModuleInitializer]")
            .AppendLineWithIndentation(indentation)
            .Append("public static void ModuleInitializer()")
            .AppendLineWithIndentation(indentation)
            .AppendSingleIndent()
            .AppendLine(
                $"=> global::Conqueror.IteratorHandlerTypeServiceRegistry.RegisterHandlerType<{handlerDescriptor.Name}>();"
            );
    }

    private static StringBuilder AppendIteratorTypeGeneratedCodeAttribute(
        this StringBuilder sb,
        Indentation indentation
    )
    {
        var version = typeof(IteratorHandlerTypeSources).Assembly.GetName().Version.ToString();

        return sb.AppendGeneratedCodeAttribute(
            indentation,
            typeof(IteratorHandlerTypeGenerator).FullName ?? "",
            version
        );
    }
}
