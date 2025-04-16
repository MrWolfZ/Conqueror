using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Conqueror.SourceGenerators.Util.Messaging;

[SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "this is common for working with string builders")]
internal static class MessageAbstractionsGenerationHelper
{
    public static (string Content, string FileName) GenerateMessageTypes(MessageTypesDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendGeneratedFile(in descriptor.MessageTypeDescriptor,
                                             in descriptor.ResponseTypeDescriptor)
                        .ToString();

        sb.Clear();

        var filename = sb.Append(descriptor.MessageTypeDescriptor.FullyQualifiedName)
                         .Append("_MessageTypes.g.cs")
                         .Replace('<', '_')
                         .Replace('>', '_')
                         .Replace(',', '.')
                         .Replace(' ', '_')
                         .ToString();

        return new(content, filename);
    }

    private static StringBuilder AppendGeneratedFile(this StringBuilder sb,
                                                     in TypeDescriptor messageTypeDescriptor,
                                                     in TypeDescriptor responseTypeDescriptor)
    {
        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(messageTypeDescriptor.Namespace) ? null : sb.AppendNamespace(indentation, messageTypeDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, messageTypeDescriptor.ParentClasses);

        using var mt = sb.AppendMessageType(indentation, in messageTypeDescriptor, in responseTypeDescriptor);

        return sb.AppendMessageTypesProperty(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                 .AppendMessageHandlerInterface(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                 .AppendMessagePipelineInterface(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                 .AppendMessageEmptyInstanceProperty(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                 .AppendMessageDefaultTypeInjector(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                 .AppendMessageTypeInjectors(indentation, in messageTypeDescriptor, in responseTypeDescriptor);
    }
}
