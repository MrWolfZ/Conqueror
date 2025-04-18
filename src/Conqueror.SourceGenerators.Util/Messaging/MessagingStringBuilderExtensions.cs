using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Util.Messaging;

internal static class MessagingStringBuilderExtensions
{
    private static readonly SHA256 Sha256 = SHA256.Create();

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

    public static StringBuilder AppendResponseTypeParameterIfNotUnitResponse(this StringBuilder sb, in TypeDescriptor responseTypeDescriptor)
    {
        return !responseTypeDescriptor.IsUnitMessageResponse() ? sb.Append($", global::{responseTypeDescriptor.FullyQualifiedName()}") : sb;
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
               .AppendIndentation(indentation)
               .Append($"public interface IHandler : global::Conqueror.IGeneratedMessageHandler<{messageTypeDescriptor.Name}")
               .AppendResponseTypeParameterIfNotUnitResponse(responseTypeDescriptor)
               .Append(", IPipeline>").AppendLine();

        using var d = sb.AppendBlock(indentation);

        return sb.AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendMessageGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"public sealed class Adapter : global::Conqueror.GeneratedMessageHandlerAdapter<{messageTypeDescriptor.Name}")
                 .AppendResponseTypeParameterIfNotUnitResponse(responseTypeDescriptor)
                 .Append(">, IHandler;").AppendLine();
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
        return sb.AppendLine()
                 .AppendMessageGeneratedCodeAttribute(indentation)
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::Conqueror.IDefaultMessageTypesInjector global::Conqueror.IMessage<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>.DefaultTypeInjector")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent()
                 .Append($"=> global::Conqueror.DefaultMessageTypesInjector<{messageTypeDescriptor.Name}")
                 .AppendResponseTypeParameterIfNotUnitResponse(responseTypeDescriptor)
                 .Append(", IHandler, IHandler.Adapter, IPipeline, IPipeline.Adapter>.Default;").AppendLine();
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

    public static StringBuilder AppendExtensionsClass(this StringBuilder sb,
                                                      Indentation indentation,
                                                      in TypeDescriptor messageTypeDescriptor,
                                                      in TypeDescriptor responseTypeDescriptor)
    {
        if (messageTypeDescriptor.Accessibility != Accessibility.Public || messageTypeDescriptor.ParentClasses.Any(pc => pc.Accessibility != Accessibility.Public))
        {
            return sb;
        }

        // hash in class name for uniqueness, even if multiple message types with the same name are in the same namespace (e.g. nested in other types)
        var uniqueMessageTypeId = Math.Abs(BitConverter.ToInt64(Sha256.ComputeHash(Encoding.UTF8.GetBytes(messageTypeDescriptor.FullyQualifiedName)), 0));

        sb = sb.AppendLine()
               .AppendMessageGeneratedCodeAttribute(indentation)
               .AppendEditorBrowsableNeverAttribute(indentation)
               .AppendIndentation(indentation)
               .Append($"public static class {messageTypeDescriptor.Name}HandlerExtensions_{uniqueMessageTypeId}").AppendLine();

        using (sb.AppendBlock(indentation))
        {
            sb = sb.AppendMessageGeneratedCodeAttribute(indentation)
                   .AppendIndentation(indentation)
                   .Append($"public static global::{messageTypeDescriptor.FullyQualifiedName}.IHandler AsIHandler(this global::Conqueror.IMessageHandler<global::{messageTypeDescriptor.FullyQualifiedName}")
                   .AppendResponseTypeParameterIfNotUnitResponse(responseTypeDescriptor)
                   .Append("> handler)").AppendLineWithIndentation(indentation)
                   .AppendSingleIndent()
                   .Append($"=> global::Conqueror.MessageHandlerExtensions.AsIHandler<global::{messageTypeDescriptor.FullyQualifiedName}")
                   .AppendResponseTypeParameterIfNotUnitResponse(responseTypeDescriptor)
                   .Append($", global::{messageTypeDescriptor.FullyQualifiedName}.IHandler>(handler);").AppendLine();
        }

        return sb;
    }

    private static StringBuilder AppendMessageGeneratedCodeAttribute(this StringBuilder sb,
                                                                     Indentation indentation)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = string.IsNullOrWhiteSpace(assemblyLocation) ? "unknown" : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return sb.AppendGeneratedCodeAttribute(indentation, "Conqueror.SourceGenerators.Messaging.MessageAbstractionsGenerator", version);
    }
}
