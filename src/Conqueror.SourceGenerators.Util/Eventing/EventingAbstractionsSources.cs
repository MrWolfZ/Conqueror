using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Conqueror.SourceGenerators.Util.Eventing;

[SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "this is common for working with string builders")]
internal static class EventingAbstractionsSources
{
    public static (string Content, string FileName) GenerateEventNotificationTypes(EventNotificationTypesDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendGeneratedFile(in descriptor)
                        .ToString();

        sb.Clear();

        var filename = sb.Append(descriptor.EventNotificationTypeDescriptor.FullyQualifiedName)
                         .Append("_EventNotificationTypes.g.cs")
                         .Replace('<', '_')
                         .Replace('>', '_')
                         .Replace(',', '.')
                         .Replace(' ', '_')
                         .ToString();

        return new(content, filename);
    }

    private static StringBuilder AppendGeneratedFile(this StringBuilder sb, in EventNotificationTypesDescriptor descriptor)
    {
        var notificationTypeDescriptor = descriptor.EventNotificationTypeDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(notificationTypeDescriptor.Namespace) ? null : sb.AppendNamespace(indentation, notificationTypeDescriptor.Namespace);

        using (sb.AppendParentClasses(indentation, notificationTypeDescriptor.ParentClasses))
        {
            using var mt = sb.AppendEventNotificationType(indentation, in notificationTypeDescriptor);

            _ = sb.AppendEventNotificationTypesProperty(indentation, in notificationTypeDescriptor)
                  .AppendEventNotificationHandlerInterface(indentation, in notificationTypeDescriptor)
                  .AppendEventNotificationEmptyInstanceProperty(indentation, in notificationTypeDescriptor)
                  .AppendEventNotificationDefaultTypeInjector(indentation, in notificationTypeDescriptor)
                  .AppendEventNotificationTypeInjectors(indentation, in notificationTypeDescriptor)
                  .AppendJsonSerializerContext(indentation, in notificationTypeDescriptor, descriptor.HasJsonSerializerContext);
        }

        return sb.AppendEventNotificationExtensionsClass(indentation, in notificationTypeDescriptor);
    }
}
