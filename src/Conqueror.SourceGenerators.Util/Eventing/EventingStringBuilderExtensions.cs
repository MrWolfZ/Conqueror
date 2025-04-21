using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Conqueror.SourceGenerators.Util.Eventing;

internal static class EventingStringBuilderExtensions
{
    public static IDisposable AppendEventNotificationType(this StringBuilder sb,
                                                          Indentation indentation,
                                                          in TypeDescriptor eventNotificationTypeDescriptor)
    {
        var keyword = eventNotificationTypeDescriptor.IsRecord ? "record" : "class";
        return sb.AppendIndentation(indentation)
                 .Append("/// <summary>").AppendLineWithIndentation(indentation)
                 .Append($"///     Event notification types for <see cref=\"global::{eventNotificationTypeDescriptor.FullyQualifiedName}\" />.").AppendLineWithIndentation(indentation)
                 .Append("/// </summary>").AppendLineWithIndentation(indentation)
                 .Append($"partial {keyword} {eventNotificationTypeDescriptor.Name} : global::Conqueror.IEventNotification<{eventNotificationTypeDescriptor.Name}>").AppendLine()
                 .AppendBlock(indentation);
    }

    public static StringBuilder AppendEventNotificationTypesProperty(this StringBuilder sb,
                                                                     Indentation indentation,
                                                                     in TypeDescriptor eventNotificationTypeDescriptor)
    {
        return sb.AppendEventingGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append("public static ").AppendNewKeywordIfNecessary(eventNotificationTypeDescriptor).Append($"global::Conqueror.EventNotificationTypes<{eventNotificationTypeDescriptor.Name}, IHandler> T => global::Conqueror.EventNotificationTypes<{eventNotificationTypeDescriptor.Name}, IHandler>.Default;").AppendLine();
    }

    public static StringBuilder AppendEventNotificationHandlerInterface(this StringBuilder sb,
                                                                        Indentation indentation,
                                                                        in TypeDescriptor eventNotificationTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendEventingGeneratedCodeAttribute(indentation)
               .AppendIndentation(indentation)
               .Append("public ").AppendNewKeywordIfNecessary(eventNotificationTypeDescriptor).Append($"partial interface IHandler : global::Conqueror.IGeneratedEventNotificationHandler<{eventNotificationTypeDescriptor.Name}, IHandler>").AppendLine();

        using var d = sb.AppendBlock(indentation);

        return sb.AppendEventingGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"global::System.Threading.Tasks.Task Handle({eventNotificationTypeDescriptor.Name} notification, global::System.Threading.CancellationToken cancellationToken = default);").AppendLine()
                 .AppendLine()
                 .AppendEventingGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Threading.Tasks.Task global::Conqueror.IGeneratedEventNotificationHandler<{eventNotificationTypeDescriptor.Name}, IHandler>.Invoke(IHandler handler, {eventNotificationTypeDescriptor.Name} notification, global::System.Threading.CancellationToken cancellationToken)").AppendLineWithIndentation(indentation)
                 .AppendSingleIndent()
                 .Append("=> handler.Handle(notification, cancellationToken);").AppendLine()
                 .AppendLine()
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendEventingGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"public sealed class Adapter : global::Conqueror.GeneratedEventNotificationHandlerAdapter<{eventNotificationTypeDescriptor.Name}, IHandler, Adapter>, IHandler;").AppendLine();
    }

    public static StringBuilder AppendEventNotificationEmptyInstanceProperty(this StringBuilder sb,
                                                                             Indentation indentation,
                                                                             in TypeDescriptor eventNotificationTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendEventingGeneratedCodeAttribute(indentation)
               .AppendEditorBrowsableNeverAttribute(indentation)
               .AppendIndentation(indentation);

        if (eventNotificationTypeDescriptor.HasProperties())
        {
            return sb.Append($"static {eventNotificationTypeDescriptor.Name}? global::Conqueror.IEventNotification<{eventNotificationTypeDescriptor.Name}>.EmptyInstance => null;").AppendLine();
        }

        return sb.Append($"static {eventNotificationTypeDescriptor.Name} global::Conqueror.IEventNotification<{eventNotificationTypeDescriptor.Name}>.EmptyInstance => new();").AppendLine();
    }

    public static StringBuilder AppendEventNotificationDefaultTypeInjector(this StringBuilder sb,
                                                                           Indentation indentation,
                                                                           in TypeDescriptor eventNotificationTypeDescriptor)
    {
        return sb.AppendLine()
                 .AppendEventingGeneratedCodeAttribute(indentation)
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::Conqueror.IDefaultEventNotificationTypesInjector global::Conqueror.IEventNotification<{eventNotificationTypeDescriptor.Name}>.DefaultTypeInjector")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent()
                 .Append($"=> global::Conqueror.DefaultEventNotificationTypesInjector<{eventNotificationTypeDescriptor.Name}, IHandler, IHandler.Adapter>.Default;").AppendLine();
    }

    public static StringBuilder AppendEventNotificationTypeInjectors(this StringBuilder sb,
                                                                     Indentation indentation,
                                                                     in TypeDescriptor eventNotificationTypeDescriptor)
    {
        return sb.AppendLine()
                 .AppendEventingGeneratedCodeAttribute(indentation)
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Collections.Generic.IReadOnlyCollection<global::Conqueror.IEventNotificationTypesInjector> global::Conqueror.IEventNotification<{eventNotificationTypeDescriptor.Name}>.TypeInjectors")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> global::Conqueror.IEventNotificationTypesInjector.GetTypeInjectorsForEventNotificationType<{eventNotificationTypeDescriptor.Name}>();").AppendLine();
    }

    public static StringBuilder AppendJsonSerializerContext(this StringBuilder sb,
                                                            Indentation indentation,
                                                            in TypeDescriptor eventNotificationTypeDescriptor,
                                                            bool hasJsonSerializerContext)
    {
        if (!hasJsonSerializerContext)
        {
            return sb;
        }

        return sb.AppendLine()
                 .AppendEventingGeneratedCodeAttribute(indentation)
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Text.Json.Serialization.JsonSerializerContext global::Conqueror.IEventNotification<{eventNotificationTypeDescriptor.Name}>.JsonSerializerContext")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> global::{eventNotificationTypeDescriptor.FullyQualifiedName}JsonSerializerContext.Default;").AppendLine();
    }

    private static StringBuilder AppendNewKeywordIfNecessary(this StringBuilder sb,
                                                             in TypeDescriptor eventNotificationTypeDescriptor)
    {
        return eventNotificationTypeDescriptor.BaseTypes.Any(t => t.Attributes.Any(a => a.BaseTypes.Any(bt => bt.FullyQualifiedName == "Conqueror.Eventing.ConquerorEventNotificationTransportAttribute"))) ? sb.Append("new ") : sb;
    }

    private static StringBuilder AppendEventingGeneratedCodeAttribute(this StringBuilder sb,
                                                                      Indentation indentation)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = string.IsNullOrWhiteSpace(assemblyLocation) ? "unknown" : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return sb.AppendGeneratedCodeAttribute(indentation, "Conqueror.SourceGenerators.Eventing.EventingAbstractionsGenerator", version);
    }
}
