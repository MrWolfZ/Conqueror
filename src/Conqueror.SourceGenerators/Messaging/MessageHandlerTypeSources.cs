using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Conqueror.SourceGenerators.Util;
using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Messaging;

public static class MessageHandlerTypeSources
{
    public static (string Content, string FileName) GenerateMessageHandlerType(MessageHandlerTypeDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendHandlerTypeFile(in descriptor)
                        .ToString();

        _ = sb.Clear();

        var filename = sb.Append(descriptor.HandlerDescriptor.FullyQualifiedName)
                         .Append("_ConquerorMessageHandlerType.g.cs")
                         .Replace("<", "__")
                         .Replace('>', '_')
                         .Replace(',', '_')
                         .Replace(' ', '_')
                         .ToString();

        return new(content, filename);
    }

    private static StringBuilder AppendHandlerTypeFile(this StringBuilder sb, in MessageHandlerTypeDescriptor descriptor)
    {
        var handlerDescriptor = descriptor.HandlerDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(handlerDescriptor.Namespace) ? null : sb.AppendNamespace(indentation, handlerDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, handlerDescriptor.ParentClasses);
        using var mt = sb.AppendMessageHandlerType(indentation, in handlerDescriptor);

        return sb.AppendGetTypeInjectorsMethod(indentation, in handlerDescriptor, in descriptor.MessageTypes)
                 .AppendModuleInitializerMethod(indentation, in handlerDescriptor);
    }

    private static IDisposable AppendMessageHandlerType(this StringBuilder sb,
                                                        Indentation indentation,
                                                        in TypeDescriptor handlerDescriptor)
    {
        var keyword = handlerDescriptor.IsRecord ? "record" : "class";
        return sb.AppendIndentation(indentation)
                 .Append($"partial {keyword} {handlerDescriptor.Name}").AppendLine()
                 .AppendBlock(indentation);
    }

    private static StringBuilder AppendGetTypeInjectorsMethod(this StringBuilder sb,
                                                              Indentation indentation,
                                                              in TypeDescriptor handlerDescriptor,
                                                              in EquatableArray<MessageTypeDescriptor> messageDescriptors)
    {
        if (handlerDescriptor.Methods.Any(m => m.Name is "GetTypeInjectors" or "Conqueror.IMessageHandler.GetTypeInjectors"))
        {
            return sb;
        }

        _ = sb.AppendMessageTypeGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append("static global::System.Collections.Generic.IEnumerable<global::Conqueror.IMessageHandlerTypesInjector> global::Conqueror.IMessageHandler.GetTypeInjectors()").AppendLine();

        using var b = sb.AppendBlock(indentation);

        string? currentMessageName = null;

        foreach (var d in from d in messageDescriptors
                          orderby d.MessageDescriptor.FullyQualifiedName
                          select d)
        {
            if (currentMessageName != d.MessageDescriptor.FullyQualifiedName)
            {
                if (currentMessageName is not null)
                {
                    _ = sb.AppendLine();
                }

                _ = sb.AppendIndentation(indentation)
                      .Append($"yield return global::{d.MessageDescriptor.FullyQualifiedName}.IHandler.CreateCoreTypesInjector<{handlerDescriptor.Name}>();").AppendLine();
            }

            currentMessageName = d.MessageDescriptor.FullyQualifiedName;

            foreach (var prefix in from a in d.Attributes
                                   where a.Prefix != "Core"
                                   orderby a.Prefix
                                   select a.Prefix)
            {
                _ = sb.AppendIndentation(indentation)
                      .Append($"yield return global::{d.MessageDescriptor.FullyQualifiedName}.IHandler.Create{prefix}TypesInjector<{handlerDescriptor.Name}>();").AppendLine();
            }
        }

        return sb;
    }

    private static StringBuilder AppendModuleInitializerMethod(this StringBuilder sb,
                                                               Indentation indentation,
                                                               in TypeDescriptor handlerDescriptor)
    {
        if (handlerDescriptor.Methods.Any(m => m.Name is "ModuleInitializer")
            || handlerDescriptor.Accessibility is not (Accessibility.Public or Accessibility.Internal)
            || handlerDescriptor.ParentClasses.Any(pc => pc.Accessibility is not (Accessibility.Public or Accessibility.Internal))
            || handlerDescriptor.IsAbstract
            || handlerDescriptor.TypeArguments.Count > 0)
        {
            return sb;
        }

        return sb.AppendLine()
                 .AppendMessageTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append("[global::System.Runtime.CompilerServices.ModuleInitializer]").AppendLineWithIndentation(indentation)
                 .Append("public static void ModuleInitializer()").AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> global::Conqueror.MessageHandlerTypeServiceRegistry.RegisterHandlerType<{handlerDescriptor.Name}>();").AppendLine();
    }

    private static StringBuilder AppendMessageTypeGeneratedCodeAttribute(this StringBuilder sb,
                                                                         Indentation indentation)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = string.IsNullOrWhiteSpace(assemblyLocation) ? "unknown" : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return sb.AppendGeneratedCodeAttribute(indentation, typeof(MessageHandlerTypeGenerator).FullName ?? string.Empty, version);
    }
}
