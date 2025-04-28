using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Conqueror.SourceGenerators.Util;

namespace Conqueror.SourceGenerators.Messaging;

public static class MessageTypeSources
{
    public static (string Content, string FileName) GenerateMessageTypeFile(MessageTypeDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendMessageTypeFile(in descriptor)
                        .ToString();

        _ = sb.Clear();

        var filename = sb.Append(descriptor.MessageDescriptor.FullyQualifiedName)
                         .Append("_ConquerorMessageType.g.cs")
                         .Replace("<", "__")
                         .Replace('>', '_')
                         .Replace(',', '_')
                         .Replace(' ', '_')
                         .ToString();

        return new(content, filename);
    }

    private static StringBuilder AppendMessageTypeFile(this StringBuilder sb, in MessageTypeDescriptor descriptor)
    {
        var messageTypeDescriptor = descriptor.MessageDescriptor;
        var responseTypeDescriptor = descriptor.ResponseDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(messageTypeDescriptor.Namespace) ? null : sb.AppendNamespace(indentation, messageTypeDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, messageTypeDescriptor.ParentClasses);
        using (sb.AppendMessageType(indentation, in messageTypeDescriptor, in responseTypeDescriptor))
        {
            _ = sb.AppendMessageTypesProperty(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                  .AppendMessageHandlerInterface(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                  .AppendMessagePipelineInterface(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                  .AppendMessageEmptyInstanceProperty(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                  .AppendJsonSerializerContext(indentation, in messageTypeDescriptor, in responseTypeDescriptor, descriptor.HasJsonSerializerContext)
                  .AppendPublicConstructorsProperty(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                  .AppendPublicPropertiesProperty(indentation, in messageTypeDescriptor, in responseTypeDescriptor);
        }

        foreach (var attribute in descriptor.Attributes)
        {
            // we are always generating the core types above, so we just skip them here
            if (attribute.Prefix == "Core")
            {
                continue;
            }

            using (sb.AppendLine().AppendTransportMessageType(indentation, in messageTypeDescriptor, in responseTypeDescriptor, in attribute))
            {
                _ = sb.AppendTransportMessageHandlerInterface(indentation, in messageTypeDescriptor, in responseTypeDescriptor, in attribute)
                      .AppendTransportMessageTypesProperties(indentation, in messageTypeDescriptor, in responseTypeDescriptor, in attribute);
            }
        }

        return sb;
    }

    private static IDisposable AppendMessageType(this StringBuilder sb,
                                                 Indentation indentation,
                                                 in TypeDescriptor messageTypeDescriptor,
                                                 in TypeDescriptor responseTypeDescriptor)
    {
        var keyword = messageTypeDescriptor.IsRecord ? "record" : "class";
        return sb.AppendIndentation(indentation)
                 .Append("/// <summary>").AppendLineWithIndentation(indentation)
                 .Append($"///     Message types for <see cref=\"global::{messageTypeDescriptor.FullyQualifiedName}\" />.").AppendLineWithIndentation(indentation)
                 .Append("/// </summary>").AppendLineWithIndentation(indentation)
                 .Append($"partial {keyword} {messageTypeDescriptor.Name} : global::Conqueror.IMessage<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}>").AppendLine()
                 .AppendBlock(indentation);
    }

    private static IDisposable AppendTransportMessageType(this StringBuilder sb,
                                                          Indentation indentation,
                                                          in TypeDescriptor messageTypeDescriptor,
                                                          in TypeDescriptor responseTypeDescriptor,
                                                          in MessageAttributeDescriptor attributeDescriptor)
    {
        var keyword = messageTypeDescriptor.IsRecord ? "record" : "class";
        var messageTypeName = attributeDescriptor.FullyQualifiedMessageTypeName ?? $"{attributeDescriptor.Namespace}.I{attributeDescriptor.Prefix}Message";
        return sb.AppendIndentation(indentation)
                 .Append($"partial {keyword} {messageTypeDescriptor.Name} : global::{messageTypeName}<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}>").AppendLine()
                 .AppendBlock(indentation);
    }

    private static StringBuilder AppendMessageTypesProperty(this StringBuilder sb,
                                                            Indentation indentation,
                                                            in TypeDescriptor messageTypeDescriptor,
                                                            in TypeDescriptor responseTypeDescriptor)
    {
        return sb.AppendMessageTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append("public static ").AppendNewKeywordIfNecessary(messageTypeDescriptor).Append($"global::Conqueror.MessageTypes<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}, IHandler> T => new();").AppendLine();
    }

    private static StringBuilder AppendMessageHandlerInterface(this StringBuilder sb,
                                                               Indentation indentation,
                                                               in TypeDescriptor messageTypeDescriptor,
                                                               in TypeDescriptor responseTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendMessageTypeGeneratedCodeAttribute(indentation)
               .AppendIndentation(indentation)
               .Append("public ").AppendNewKeywordIfNecessary(messageTypeDescriptor).Append($"partial interface IHandler : global::Conqueror.IMessageHandler<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}, IHandler, IHandler.Proxy, IPipeline, IPipeline.Proxy>").AppendLine();

        using var d = sb.AppendBlock(indentation);

        _ = sb.AppendMessageTypeGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append("global::System.Threading.Tasks.Task").AppendLine()
              .AppendResponseTypeParameterIfNotUnitResponse(in responseTypeDescriptor)
              .Append($" Handle({messageTypeDescriptor.Name} message, global::System.Threading.CancellationToken cancellationToken = default);").AppendLine()
              .AppendLine()
              .AppendMessageTypeGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append("static ").Append(responseTypeDescriptor.IsUnitMessageResponse() ? "async " : string.Empty)
              .Append($"global::System.Threading.Tasks.Task<{responseTypeDescriptor.FullyQualifiedName()}> global::Conqueror.IMessageHandler<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}, IHandler>.Invoke(IHandler handler, {messageTypeDescriptor.Name} message, global::System.Threading.CancellationToken cancellationToken)").AppendLine();

        if (responseTypeDescriptor.IsUnitMessageResponse())
        {
            using (sb.AppendBlock(indentation))
            {
                _ = sb.AppendIndentation(indentation)
                      .Append("await handler.Handle(message, cancellationToken).ConfigureAwait(false);").AppendLineWithIndentation(indentation)
                      .Append("return global::Conqueror.UnitMessageResponse.Instance;").AppendLine();
            }
        }
        else
        {
            _ = sb.AppendIndentation(indentation)
                  .AppendSingleIndent()
                  .Append("=> handler.Handle(message, cancellationToken);").AppendLine();
        }

        return sb.AppendLine()
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendMessageTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"public sealed class Proxy : global::Conqueror.MessageHandlerProxy<{messageTypeDescriptor.Name}")
                 .AppendResponseTypeParameterToListIfNotUnitResponse(in responseTypeDescriptor)
                 .Append(", IHandler, Proxy>, IHandler;").AppendLine();
    }

    private static StringBuilder AppendTransportMessageHandlerInterface(this StringBuilder sb,
                                                                        Indentation indentation,
                                                                        in TypeDescriptor messageTypeDescriptor,
                                                                        in TypeDescriptor responseTypeDescriptor,
                                                                        in MessageAttributeDescriptor attributeDescriptor)
    {
        return sb.AppendIndentation(indentation)
                 .Append($"partial interface IHandler : global::{attributeDescriptor.Namespace}.I{attributeDescriptor.Prefix}MessageHandler<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}, IHandler>;").AppendLine();
    }

    private static StringBuilder AppendMessagePipelineInterface(this StringBuilder sb,
                                                                Indentation indentation,
                                                                in TypeDescriptor messageTypeDescriptor,
                                                                in TypeDescriptor responseTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendMessageTypeGeneratedCodeAttribute(indentation)
               .AppendIndentation(indentation)
               .Append("public ").AppendNewKeywordIfNecessary(messageTypeDescriptor).Append($"partial interface IPipeline : global::Conqueror.IMessagePipeline<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}>").AppendLine();

        using var d = sb.AppendBlock(indentation);

        return sb.AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendMessageTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"public sealed class Proxy : global::Conqueror.MessagePipelineProxy<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}>, IPipeline;").AppendLine();
    }

    private static StringBuilder AppendMessageEmptyInstanceProperty(this StringBuilder sb,
                                                                    Indentation indentation,
                                                                    in TypeDescriptor messageTypeDescriptor,
                                                                    in TypeDescriptor responseTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendMessageTypeGeneratedCodeAttribute(indentation)
               .AppendIndentation(indentation);

        if (messageTypeDescriptor.HasProperties() || messageTypeDescriptor.IsAbstract)
        {
            return sb.Append($"static {messageTypeDescriptor.Name}? global::Conqueror.IMessage<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}>.EmptyInstance => null;").AppendLine();
        }

        return sb.Append($"static {messageTypeDescriptor.Name} global::Conqueror.IMessage<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}>.EmptyInstance => new();").AppendLine();
    }

    private static StringBuilder AppendAttributeParameterProperty(this StringBuilder sb,
                                                                  Indentation indentation,
                                                                  in TypeDescriptor messageTypeDescriptor,
                                                                  in TypeDescriptor responseTypeDescriptor,
                                                                  in MessageAttributeDescriptor attributeDescriptor,
                                                                  in AttributeParameterDescriptor parameterDescriptor)
    {
        var messageTypeName = attributeDescriptor.FullyQualifiedMessageTypeName ?? $"{attributeDescriptor.Namespace}.I{attributeDescriptor.Prefix}Message";
        return sb.AppendMessageTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append("static ")
                 .AppendAttributeParameterPropertyType(in parameterDescriptor)
                 .Append($" global::{messageTypeName}<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}>.{parameterDescriptor.Name} => ")
                 .AppendAttributeParameterValue(in parameterDescriptor.Value).Append(";").AppendLine();
    }

    private static StringBuilder AppendJsonSerializerContext(this StringBuilder sb,
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
                 .AppendMessageTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Text.Json.Serialization.JsonSerializerContext global::Conqueror.IMessage<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}>.JsonSerializerContext")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> global::{messageTypeDescriptor.FullyQualifiedName}JsonSerializerContext.Default;").AppendLine();
    }

    private static StringBuilder AppendPublicConstructorsProperty(this StringBuilder sb,
                                                                  Indentation indentation,
                                                                  in TypeDescriptor messageTypeDescriptor,
                                                                  in TypeDescriptor responseTypeDescriptor)
    {
        return sb.AppendLine()
                 .AppendMessageTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Collections.Generic.IEnumerable<global::System.Reflection.ConstructorInfo> global::Conqueror.IMessage<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}>.PublicConstructors")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> typeof({messageTypeDescriptor.Name}).GetConstructors(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance);").AppendLine();
    }

    private static StringBuilder AppendPublicPropertiesProperty(this StringBuilder sb,
                                                                Indentation indentation,
                                                                in TypeDescriptor messageTypeDescriptor,
                                                                in TypeDescriptor responseTypeDescriptor)
    {
        return sb.AppendLine()
                 .AppendMessageTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Collections.Generic.IEnumerable<global::System.Reflection.PropertyInfo> global::Conqueror.IMessage<{messageTypeDescriptor.Name}, {responseTypeDescriptor.FullyQualifiedName()}>.PublicProperties")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> typeof({messageTypeDescriptor.Name}).GetProperties(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance);").AppendLine();
    }

    private static StringBuilder AppendResponseTypeParameterIfNotUnitResponse(this StringBuilder sb, in TypeDescriptor responseTypeDescriptor)
    {
        return !responseTypeDescriptor.IsUnitMessageResponse() ? sb.Append($"<{responseTypeDescriptor.FullyQualifiedName()}>") : sb;
    }

    private static StringBuilder AppendResponseTypeParameterToListIfNotUnitResponse(this StringBuilder sb, in TypeDescriptor responseTypeDescriptor)
    {
        return !responseTypeDescriptor.IsUnitMessageResponse() ? sb.Append($", {responseTypeDescriptor.FullyQualifiedName()}") : sb;
    }

    private static StringBuilder AppendNewKeywordIfNecessary(this StringBuilder sb,
                                                             in TypeDescriptor messageTypeDescriptor)
    {
        return messageTypeDescriptor.BaseTypes.Any(t => t.Attributes.Any(a => a.Attributes.Any(bt => bt.FullyQualifiedName == "Conqueror.Messaging.MessageTransportAttribute"))) ? sb.Append("new ") : sb;
    }

    private static StringBuilder AppendTransportMessageTypesProperties(this StringBuilder sb,
                                                                       Indentation indentation,
                                                                       in TypeDescriptor messageTypeDescriptor,
                                                                       in TypeDescriptor responseTypeDescriptor,
                                                                       in MessageAttributeDescriptor attributeDescriptor)
    {
        foreach (var property in attributeDescriptor.Properties)
        {
            _ = sb.AppendLine()
                  .AppendAttributeParameterProperty(indentation, in messageTypeDescriptor, in responseTypeDescriptor, in attributeDescriptor, in property);
        }

        return sb;
    }

    private static StringBuilder AppendAttributeParameterPropertyType(this StringBuilder sb, in AttributeParameterDescriptor parameterDescriptor)
    {
        if (parameterDescriptor is { IsPrimitive: false, IsArray: false })
        {
            _ = sb.Append("global::");
        }

        _ = sb.Append(parameterDescriptor.FullyQualifiedTypeName);

        if (parameterDescriptor.Value.IsNull)
        {
            _ = sb.Append('?');
        }

        return sb;
    }

    private static StringBuilder AppendAttributeParameterValue(this StringBuilder sb, in AttributeParameterValueDescriptor valueDescriptor)
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
                    _ = sb.Append(',');
                }

                _ = sb.Append(' ');
            }

            return sb.Append("}");
        }

        return sb.Append(valueDescriptor.Value);
    }

    private static StringBuilder AppendMessageTypeGeneratedCodeAttribute(this StringBuilder sb,
                                                                         Indentation indentation)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = string.IsNullOrWhiteSpace(assemblyLocation) ? "unknown" : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return sb.AppendGeneratedCodeAttribute(indentation, typeof(MessageTypeGenerator).FullName ?? string.Empty, version);
    }
}
