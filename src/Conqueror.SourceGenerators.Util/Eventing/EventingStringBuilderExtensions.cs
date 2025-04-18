using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

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
                 .Append("public static ").AppendNewKeywordIfNecessary(eventNotificationTypeDescriptor).Append($"global::Conqueror.EventNotificationTypes<{eventNotificationTypeDescriptor.Name}> T => global::Conqueror.EventNotificationTypes<{eventNotificationTypeDescriptor.Name}>.Default;").AppendLine();
    }

    public static StringBuilder AppendEventNotificationHandlerInterface(this StringBuilder sb,
                                                                        Indentation indentation,
                                                                        in TypeDescriptor eventNotificationTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendEventingGeneratedCodeAttribute(indentation)
               .AppendIndentation(indentation)
               .Append("public ").AppendNewKeywordIfNecessary(eventNotificationTypeDescriptor).Append($"interface IHandler : global::Conqueror.IGeneratedEventNotificationHandler<{eventNotificationTypeDescriptor.Name}>").AppendLine();

        using var d = sb.AppendBlock(indentation);

        return sb.AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendEventingGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"public sealed class Adapter : global::Conqueror.GeneratedEventNotificationHandlerAdapter<{eventNotificationTypeDescriptor.Name}>, IHandler;").AppendLine();
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

    public static StringBuilder AppendEventNotificationExtensionsClass(this StringBuilder sb,
                                                                       Indentation indentation,
                                                                       in TypeDescriptor eventNotificationTypeDescriptor)
    {
        if (eventNotificationTypeDescriptor.Accessibility != Accessibility.Public || eventNotificationTypeDescriptor.ParentClasses.Any(pc => pc.Accessibility != Accessibility.Public))
        {
            return sb;
        }

        sb = sb.AppendLine()
               .AppendEventingGeneratedCodeAttribute(indentation)
               .AppendEditorBrowsableNeverAttribute(indentation)
               .AppendIndentation(indentation)
               .Append("public static class ")
               .AppendTypeNameWithInlinedTypeArguments(in eventNotificationTypeDescriptor)
               .Append("HandlerExtensions_")

               // hash in class name for uniqueness, even if multiple event notification types with the same name are in the same namespace (e.g. nested in other types)
               .AppendHash(eventNotificationTypeDescriptor.FullyQualifiedName).AppendLine();

        using (sb.AppendBlock(indentation))
        {
            sb = sb.AppendEventingGeneratedCodeAttribute(indentation)
                   .AppendIndentation(indentation)
                   .Append($"public static global::{eventNotificationTypeDescriptor.FullyQualifiedName}.IHandler AsIHandler")
                   .AppendTypeArguments(in eventNotificationTypeDescriptor)
                   .Append($"(this global::Conqueror.IEventNotificationHandler<global::{eventNotificationTypeDescriptor.FullyQualifiedName}> handler)")
                   .AppendConstraintClauses(indentation, in eventNotificationTypeDescriptor)
                   .AppendLineWithIndentation(indentation)
                   .AppendSingleIndent()
                   .Append($"=> global::Conqueror.EventNotificationHandlerExtensions.AsIHandler<global::{eventNotificationTypeDescriptor.FullyQualifiedName}, global::{eventNotificationTypeDescriptor.FullyQualifiedName}.IHandler>(handler);").AppendLine();
        }

        return sb;
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
