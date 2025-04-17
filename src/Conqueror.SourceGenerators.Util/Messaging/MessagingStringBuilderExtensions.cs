using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Conqueror.SourceGenerators.Util.Messaging;

internal static class MessagingStringBuilderExtensions
{
    public static IDisposable AppendMessageType(this StringBuilder sb,
                                                Indentation indentation,
                                                in TypeDescriptor messageTypeDescriptor,
                                                in TypeDescriptor responseTypeDescriptor)
    {
        var keyword = messageTypeDescriptor.IsRecord ? "record" : "class";
        return sb.AppendIndentation(indentation)
                 .Append("/// <summary>").AppendLineWithIndentation(indentation)
                 .Append($"///     Message Types for <see cref=\"global::{messageTypeDescriptor.FullyQualifiedName}\" />.").AppendLineWithIndentation(indentation)
                 .Append("/// </summary>").AppendLineWithIndentation(indentation)
                 .Append($"partial {keyword} {messageTypeDescriptor.Name} : global::Conqueror.IMessage<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>").AppendLine()
                 .AppendBlock(indentation);
    }

    public static StringBuilder AppendMessageTypesProperty(this StringBuilder sb,
                                                           Indentation indentation,
                                                           in TypeDescriptor messageTypeDescriptor,
                                                           in TypeDescriptor responseTypeDescriptor)
    {
        return sb.AppendMessageGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"public static global::Conqueror.MessageTypes<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}> T => global::Conqueror.MessageTypes<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>.Default;").AppendLine();
    }

    public static StringBuilder AppendMessageHandlerInterface(this StringBuilder sb,
                                                              Indentation indentation,
                                                              in TypeDescriptor messageTypeDescriptor,
                                                              in TypeDescriptor responseTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendMessageGeneratedCodeAttribute(indentation)
               .AppendIndentation(indentation);

        if (responseTypeDescriptor.IsUnitMessageResponse())
        {
            return sb.Append($"public interface IHandler : global::Conqueror.IGeneratedMessageHandler<{messageTypeDescriptor.Name}, IPipeline>;").AppendLine();
        }

        return sb.Append($"public interface IHandler : global::Conqueror.IGeneratedMessageHandler<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}, IPipeline>;").AppendLine();
    }

    public static StringBuilder AppendMessagePipelineInterface(this StringBuilder sb,
                                                               Indentation indentation,
                                                               in TypeDescriptor messageTypeDescriptor,
                                                               in TypeDescriptor responseTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendMessageGeneratedCodeAttribute(indentation)
               .AppendIndentation(indentation)
               .Append($"public interface IPipeline : global::Conqueror.IMessagePipeline<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>").AppendLine();

        using var d = sb.AppendBlock(indentation);

        return sb.AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendMessageGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"public sealed class Adapter : global::Conqueror.GeneratedMessagePipelineAdapter<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>, IPipeline;").AppendLine();
    }

    public static StringBuilder AppendMessageEmptyInstanceProperty(this StringBuilder sb,
                                                                   Indentation indentation,
                                                                   in TypeDescriptor messageTypeDescriptor,
                                                                   in TypeDescriptor responseTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendMessageGeneratedCodeAttribute(indentation)
               .AppendEditorBrowsableNeverAttribute(indentation)
               .AppendIndentation(indentation);

        if (messageTypeDescriptor.HasProperties)
        {
            return sb.Append($"static {messageTypeDescriptor.Name}? global::Conqueror.IMessage<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>.EmptyInstance => null;").AppendLine();
        }

        return sb.Append($"static {messageTypeDescriptor.Name} global::Conqueror.IMessage<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>.EmptyInstance => new();").AppendLine();
    }

    public static StringBuilder AppendMessageDefaultTypeInjector(this StringBuilder sb,
                                                                 Indentation indentation,
                                                                 in TypeDescriptor messageTypeDescriptor,
                                                                 in TypeDescriptor responseTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendMessageGeneratedCodeAttribute(indentation)
               .AppendEditorBrowsableNeverAttribute(indentation)
               .AppendIndentation(indentation)
               .Append($"static global::Conqueror.IDefaultMessageTypesInjector global::Conqueror.IMessage<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>.DefaultTypeInjector")
               .AppendLineWithIndentation(indentation)
               .AppendSingleIndent();

        if (responseTypeDescriptor.IsUnitMessageResponse())
        {
            return sb.Append($"=> global::Conqueror.DefaultMessageTypesInjector<{messageTypeDescriptor.Name}, IPipeline, IPipeline.Adapter>.Default;").AppendLine();
        }

        return sb.Append($"=> global::Conqueror.DefaultMessageTypesInjector<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}, IPipeline, IPipeline.Adapter>.Default;").AppendLine();
    }

    public static StringBuilder AppendMessageTypeInjectors(this StringBuilder sb,
                                                           Indentation indentation,
                                                           in TypeDescriptor messageTypeDescriptor,
                                                           in TypeDescriptor responseTypeDescriptor)
    {
        return sb.AppendLine()
                 .AppendMessageGeneratedCodeAttribute(indentation)
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Collections.Generic.IReadOnlyCollection<global::Conqueror.IMessageTypesInjector> global::Conqueror.IMessage<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>.TypeInjectors")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> global::Conqueror.IMessageTypesInjector.GetTypeInjectorsForMessageType<{messageTypeDescriptor.Name}>();").AppendLine();
    }

    public static StringBuilder AppendJsonSerializerContext(this StringBuilder sb,
                                                            Indentation indentation,
                                                            in TypeDescriptor messageTypeDescriptor,
                                                            in TypeDescriptor responseTypeDescriptor,
                                                            bool hasJsonSerializerContext)
    {
        if (!hasJsonSerializerContext)
        {
            return sb;
        }

        return sb.AppendLine()
                 .AppendMessageGeneratedCodeAttribute(indentation)
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Text.Json.Serialization.JsonSerializerContext global::Conqueror.IMessage<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>.JsonSerializerContext")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> global::{messageTypeDescriptor.FullyQualifiedName}JsonSerializerContext.Default;").AppendLine();
    }

    private static StringBuilder AppendMessageGeneratedCodeAttribute(this StringBuilder sb,
                                                                     Indentation indentation)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = string.IsNullOrWhiteSpace(assemblyLocation) ? "unknown" : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return sb.AppendGeneratedCodeAttribute(indentation, "Conqueror.SourceGenerators.Messaging.MessageAbstractionsGenerator", version);
    }
}
