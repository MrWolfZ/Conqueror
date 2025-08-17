namespace Conqueror.SourceGenerators.Messaging;

using Microsoft.CodeAnalysis;

internal static class MessageHandlerTypeSources
{
    public static (string Content, string FileName) GenerateMessageHandlerType(MessageHandlerTypeDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendHandlerTypeFile(in descriptor).ToString();

        _ = sb.Clear();

        var filename = sb.Append(descriptor.HandlerDescriptor.FullyQualifiedName)
            .Append("_ConquerorMessageHandlerType.g.cs")
            .Replace("<", "__")
            .Replace(oldChar: '>', newChar: '_')
            .Replace(oldChar: ',', newChar: '_')
            .Replace(oldChar: ' ', newChar: '_')
            .ToString();

        return (content, filename);
    }

    private static StringBuilder AppendHandlerTypeFile(
        this StringBuilder sb,
        in MessageHandlerTypeDescriptor descriptor
    )
    {
        var handlerDescriptor = descriptor.HandlerDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(handlerDescriptor.Namespace)
            ? null
            : sb.AppendNamespace(indentation, handlerDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, handlerDescriptor.ParentClasses);
        using var mt = sb.AppendMessageHandlerType(indentation, in handlerDescriptor, in descriptor.MessageTypes);

        return sb.AppendGetTypeInjectorsMethod(indentation, in handlerDescriptor, in descriptor.MessageTypes)
            .AppendModuleInitializerMethod(indentation, in handlerDescriptor);
    }

    private static IDisposable AppendMessageHandlerType(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor handlerDescriptor,
        in EquatableArray<MessageTypeDescriptor> messageDescriptors
    )
    {
        var keyword = handlerDescriptor.IsRecord ? "record" : "class";
        _ = sb.AppendIndentation(indentation)
            .Append(
                $"partial {keyword} {handlerDescriptor.Name} : global::Conqueror.IMessageHandlerWithSourceGeneration"
            );

        foreach (
            var (ns, prefix) in messageDescriptors
                .OrderBy(d => d.MessageDescriptor.FullyQualifiedName, StringComparer.OrdinalIgnoreCase)
                .SelectMany(d => d.Attributes, (d, a) => new { d, a })
                .Where(t => !string.Equals(t.a.Prefix, "Core", StringComparison.Ordinal))
                .Select(t => (t.a.Namespace, t.a.Prefix))
                .Distinct()
        )
        {
            _ = sb.Append($", global::{ns}.I{prefix}MessageHandler");
        }

        return sb.AppendLine().AppendBlock(indentation);
    }

    private static StringBuilder AppendGetTypeInjectorsMethod(
        this StringBuilder sb,
        Indentation indentation,
        in TypeDescriptor handlerDescriptor,
        in EquatableArray<MessageTypeDescriptor> messageDescriptors
    )
    {
        if (
            handlerDescriptor.Methods.Any(m =>
                m.Name.Equals("GetTypeInjectors", StringComparison.Ordinal)
                || m.Name.Equals("Conqueror.IMessageHandler.GetTypeInjectors", StringComparison.Ordinal)
            )
        )
        {
            return sb;
        }

        _ = sb.AppendMessageTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .AppendLine(
                "static global::System.Collections.Generic.IEnumerable<global::Conqueror.IMessageHandlerTypesInjector> global::Conqueror.IMessageHandler.GetTypeInjectors()"
            );

        using var b = sb.AppendBlock(indentation);

        string? currentMessageName = null;

        foreach (
            var d in messageDescriptors.OrderBy(
                d => d.MessageDescriptor.FullyQualifiedName,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            if (!string.Equals(currentMessageName, d.MessageDescriptor.FullyQualifiedName, StringComparison.Ordinal))
            {
                if (currentMessageName is not null)
                {
                    _ = sb.AppendLine();
                }

                _ = sb.AppendIndentation(indentation)
                    .AppendLine(
                        $"yield return global::{d.MessageDescriptor.FullyQualifiedName}.IHandler.CreateCoreTypesInjector<{handlerDescriptor.Name}>();"
                    );
            }

            currentMessageName = d.MessageDescriptor.FullyQualifiedName;

            foreach (
                var prefix in d
                    .Attributes.Where(a => !string.Equals(a.Prefix, "Core", StringComparison.Ordinal))
                    .OrderBy(a => a.Prefix, StringComparer.OrdinalIgnoreCase)
                    .Select(a => a.Prefix)
            )
            {
                _ = sb.AppendIndentation(indentation)
                    .AppendLine(
                        $"yield return global::{d.MessageDescriptor.FullyQualifiedName}.IHandler.Create{prefix}TypesInjector<{handlerDescriptor.Name}>();"
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
            .AppendMessageTypeGeneratedCodeAttribute(indentation)
            .AppendIndentation(indentation)
            .Append("[global::System.Runtime.CompilerServices.ModuleInitializer]")
            .AppendLineWithIndentation(indentation)
            .Append("public static void ModuleInitializer()")
            .AppendLineWithIndentation(indentation)
            .AppendSingleIndent()
            .AppendLine(
                $"=> global::Conqueror.MessageHandlerTypeServiceRegistry.RegisterHandlerType<{handlerDescriptor.Name}>();"
            );
    }

    private static StringBuilder AppendMessageTypeGeneratedCodeAttribute(this StringBuilder sb, Indentation indentation)
    {
        var version = typeof(MessageHandlerTypeSources).Assembly.GetName().Version.ToString();

        return sb.AppendGeneratedCodeAttribute(
            indentation,
            typeof(MessageHandlerTypeGenerator).FullName ?? "",
            version
        );
    }
}
