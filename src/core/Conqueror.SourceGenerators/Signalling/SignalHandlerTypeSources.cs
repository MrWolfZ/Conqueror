namespace Conqueror.SourceGenerators.Signalling;

using Microsoft.CodeAnalysis;

internal static class SignalHandlerTypeSources
{
    public static (string Content, string FileName) GenerateSignalHandlerType(SignalHandlerTypeDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendHandlerTypeFile(in descriptor).ToString();

        _ = sb.Clear();

        var filename = sb.Append(descriptor.HandlerDescriptor.FullyQualifiedName)
            .Append("_ConquerorSignalHandlerType.g.cs")
            .Replace(oldChar: '<', newChar: '_')
            .Replace(oldChar: '>', newChar: '_')
            .Replace(oldChar: ',', newChar: '.')
            .Replace(oldChar: ' ', newChar: '_')
            .ToString();

        return (content, filename);
    }

    private static StringBuilder AppendHandlerTypeFile(this StringBuilder sb, in SignalHandlerTypeDescriptor descriptor)
    {
        var handlerDescriptor = descriptor.HandlerDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(handlerDescriptor.Namespace)
            ? null
            : sb.AppendNamespace(indentation, handlerDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, handlerDescriptor.ParentClasses);
        using var mt = sb.AppendSignalHandlerType(indentation, in handlerDescriptor, in descriptor.SignalTypes);

        return sb.AppendGetTypeInjectorsMethod(indentation, in handlerDescriptor, in descriptor.SignalTypes)
            .AppendModuleInitializerMethod(indentation, in handlerDescriptor);
    }

    private static IDisposable AppendSignalHandlerType(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor handlerDescriptor,
        in EquatableArray<SignalTypeDescriptor> signalDescriptors
    )
    {
        var keyword = handlerDescriptor.IsRecord ? "record" : "class";
        _ = sb.AppendIndentation(indentation)
            .Append(
                $"partial {keyword} {handlerDescriptor.Name} : global::Conqueror.ISignalHandlerWithSourceGeneration"
            );

        foreach (
            var (ns, prefix) in signalDescriptors
                .OrderBy(d => d.SignalDescriptor.FullyQualifiedName, StringComparer.OrdinalIgnoreCase)
                .SelectMany(d => d.Attributes, (d, a) => new { d, a })
                .Where(t => !string.Equals(t.a.Prefix, "Core", StringComparison.Ordinal))
                .Select(t => (t.a.Namespace, t.a.Prefix))
                .Distinct()
        )
        {
            _ = sb.Append($", global::{ns}.I{prefix}SignalHandler");
        }

        return sb.AppendLine().AppendBlock(indentation);
    }

    private static StringBuilder AppendGetTypeInjectorsMethod(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor handlerDescriptor,
        in EquatableArray<SignalTypeDescriptor> signalDescriptors
    )
    {
        if (
            handlerDescriptor.Methods.Any(m =>
                string.Equals(m.Name, "GetTypeInjectors", StringComparison.Ordinal)
                || string.Equals(m.Name, "Conqueror.ISignalHandler.GetTypeInjectors", StringComparison.Ordinal)
            )
        )
        {
            return sb;
        }

        _ = sb.AppendSignalTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .AppendLine(
                "static global::System.Collections.Generic.IEnumerable<global::Conqueror.ISignalHandlerTypesInjector> global::Conqueror.ISignalHandler.GetTypeInjectors()"
            );

        using var b = sb.AppendBlock(indentation);

        string? currentSignalName = null;

        foreach (
            var d in signalDescriptors.OrderBy(
                d => d.SignalDescriptor.FullyQualifiedName,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            if (!string.Equals(currentSignalName, d.SignalDescriptor.FullyQualifiedName, StringComparison.Ordinal))
            {
                if (currentSignalName is not null)
                {
                    _ = sb.AppendLine();
                }

                _ = sb.AppendIndentation(indentation)
                    .AppendLine(
                        $"yield return global::{d.SignalDescriptor.FullyQualifiedName}.IHandler.CreateCoreTypesInjector<{handlerDescriptor.Name}>();"
                    );
            }

            currentSignalName = d.SignalDescriptor.FullyQualifiedName;

            foreach (
                var prefix in d
                    .Attributes.Where(a => !string.Equals(a.Prefix, "Core", StringComparison.Ordinal))
                    .OrderBy(a => a.Prefix, StringComparer.OrdinalIgnoreCase)
                    .Select(a => a.Prefix)
            )
            {
                _ = sb.AppendIndentation(indentation)
                    .AppendLine(
                        $"yield return global::{d.SignalDescriptor.FullyQualifiedName}.IHandler.Create{prefix}TypesInjector<{handlerDescriptor.Name}>();"
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
            handlerDescriptor.Methods.Any(m => string.Equals(m.Name, "ModuleInitializer", StringComparison.Ordinal))
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
            .AppendSignalTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .Append("[global::System.Runtime.CompilerServices.ModuleInitializer]")
            .AppendLineWithIndentation(indentation)
            .Append("public static void ModuleInitializer()")
            .AppendLineWithIndentation(indentation)
            .AppendSingleIndent()
            .AppendLine(
                $"=> global::Conqueror.SignalHandlerTypeServiceRegistry.RegisterHandlerType<{handlerDescriptor.Name}>();"
            );
    }

    private static StringBuilder AppendSignalTypeGeneratedCodeAttribute(this StringBuilder sb, Indentation indentation)
    {
        var version = typeof(SignalHandlerTypeSources).Assembly.GetName().Version.ToString();

        return sb.AppendGeneratedCodeAttribute(indentation, typeof(SignalHandlerTypeGenerator).FullName ?? "", version);
    }
}
