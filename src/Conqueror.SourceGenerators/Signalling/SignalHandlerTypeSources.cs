using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Conqueror.SourceGenerators.Util;

namespace Conqueror.SourceGenerators.Signalling;

[SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "this is common for working with string builders")]
public static class SignalHandlerTypeSources
{
    public static (string Content, string FileName) GenerateSignalHandlerType(SignalHandlerTypeDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendHandlerTypeFile(in descriptor)
                        .ToString();

        sb.Clear();

        var filename = sb.Append(descriptor.HandlerDescriptor.FullyQualifiedName)
                         .Append("_ConquerorSignalHandlerType.g.cs")
                         .Replace('<', '_')
                         .Replace('>', '_')
                         .Replace(',', '.')
                         .Replace(' ', '_')
                         .ToString();

        return new(content, filename);
    }

    private static StringBuilder AppendHandlerTypeFile(this StringBuilder sb, in SignalHandlerTypeDescriptor descriptor)
    {
        var handlerDescriptor = descriptor.HandlerDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(handlerDescriptor.Namespace) ? null : sb.AppendNamespace(indentation, handlerDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, handlerDescriptor.ParentClasses);
        using var mt = sb.AppendSignalHandlerType(indentation, in handlerDescriptor);

        return sb.AppendGetTypeInjectorsMethod(indentation, in handlerDescriptor, in descriptor.SignalTypes);
    }

    private static IDisposable AppendSignalHandlerType(this StringBuilder sb,
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
                                                              in EquatableArray<SignalTypeDescriptor> signalDescriptors)
    {
        if (handlerDescriptor.Methods.Any(m => m.Name is "GetTypeInjectors" or "Conqueror.ISignalHandler.GetTypeInjectors"))
        {
            return sb;
        }

        _ = sb.AppendSignalTypeGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append("static global::System.Collections.Generic.IEnumerable<global::Conqueror.ISignalHandlerTypesInjector> global::Conqueror.ISignalHandler.GetTypeInjectors()").AppendLine();

        using var b = sb.AppendBlock(indentation);

        string? currentSignalName = null;

        foreach (var d in from d in signalDescriptors
                          orderby d.SignalDescriptor.FullyQualifiedName
                          select d)
        {
            if (currentSignalName != d.SignalDescriptor.FullyQualifiedName)
            {
                if (currentSignalName is not null)
                {
                    _ = sb.AppendLine();
                }

                _ = sb.AppendIndentation(indentation)
                      .Append($"yield return global::{d.SignalDescriptor.FullyQualifiedName}.IHandler.CreateCoreTypesInjector<{handlerDescriptor.Name}>();").AppendLine();
            }

            currentSignalName = d.SignalDescriptor.FullyQualifiedName;

            foreach (var prefix in from a in d.Attributes
                                   where a.Prefix != "Core"
                                   orderby a.Prefix
                                   select a.Prefix)
            {
                _ = sb.AppendIndentation(indentation)
                      .Append($"yield return global::{d.SignalDescriptor.FullyQualifiedName}.IHandler.Create{prefix}TypesInjector<{handlerDescriptor.Name}>();").AppendLine();
            }
        }

        return sb;
    }

    private static StringBuilder AppendSignalTypeGeneratedCodeAttribute(this StringBuilder sb,
                                                                        Indentation indentation)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = string.IsNullOrWhiteSpace(assemblyLocation) ? "unknown" : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return sb.AppendGeneratedCodeAttribute(indentation, typeof(SignalHandlerTypeGenerator).FullName ?? string.Empty, version);
    }
}
