using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Conqueror.SourceGenerators.Util;

namespace Conqueror.SourceGenerators.Signalling;

public static class SignalTypeSources
{
    public static (string Content, string FileName) GenerateSignalTypeFile(SignalTypeDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendSignalTypeFile(in descriptor)
                        .ToString();

        _ = sb.Clear();

        var filename = sb.Append(descriptor.SignalDescriptor.FullyQualifiedName)
                         .Append("_ConquerorSignalType.g.cs")
                         .Replace('<', '_')
                         .Replace('>', '_')
                         .Replace(',', '.')
                         .Replace(' ', '_')
                         .ToString();

        return new(content, filename);
    }

    private static StringBuilder AppendSignalTypeFile(this StringBuilder sb, in SignalTypeDescriptor descriptor)
    {
        var signalTypeDescriptor = descriptor.SignalDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(signalTypeDescriptor.Namespace) ? null : sb.AppendNamespace(indentation, signalTypeDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, signalTypeDescriptor.ParentClasses);
        using (sb.AppendSignalType(indentation, in signalTypeDescriptor))
        {
            _ = sb.AppendSignalTypesProperty(indentation, in signalTypeDescriptor)
                  .AppendSignalHandlerInterface(indentation, in signalTypeDescriptor)
                  .AppendSignalEmptyInstanceProperty(indentation, in signalTypeDescriptor)
                  .AppendJsonSerializerContext(indentation, in signalTypeDescriptor, descriptor.HasJsonSerializerContext)
                  .AppendPublicConstructorsProperty(indentation, in signalTypeDescriptor)
                  .AppendPublicPropertiesProperty(indentation, in signalTypeDescriptor);
        }

        foreach (var attribute in descriptor.Attributes)
        {
            // we are always generating the core types above, so we just skip them here
            if (attribute.Prefix == "Core")
            {
                continue;
            }

            using (sb.AppendLine().AppendTransportSignalType(indentation, in signalTypeDescriptor, in attribute))
            {
                _ = sb.AppendTransportSignalHandlerInterface(indentation, in signalTypeDescriptor, in attribute)
                      .AppendTransportSignalTypesProperties(indentation, in signalTypeDescriptor, in attribute);
            }
        }

        return sb;
    }

    private static IDisposable AppendSignalType(this StringBuilder sb,
                                                Indentation indentation,
                                                in TypeDescriptor signalTypeDescriptor)
    {
        var keyword = signalTypeDescriptor.IsRecord ? "record" : "class";
        return sb.AppendIndentation(indentation)
                 .Append("/// <summary>").AppendLineWithIndentation(indentation)
                 .Append($"///     Signal types for <see cref=\"global::{signalTypeDescriptor.FullyQualifiedName}\" />.").AppendLineWithIndentation(indentation)
                 .Append("/// </summary>").AppendLineWithIndentation(indentation)
                 .Append($"partial {keyword} {signalTypeDescriptor.Name} : global::Conqueror.ISignal<{signalTypeDescriptor.Name}>").AppendLine()
                 .AppendBlock(indentation);
    }

    private static IDisposable AppendTransportSignalType(this StringBuilder sb,
                                                         Indentation indentation,
                                                         in TypeDescriptor signalTypeDescriptor,
                                                         in SignalAttributeDescriptor attributeDescriptor)
    {
        var keyword = signalTypeDescriptor.IsRecord ? "record" : "class";
        var signalTypeName = attributeDescriptor.FullyQualifiedSignalTypeName ?? $"{attributeDescriptor.Namespace}.I{attributeDescriptor.Prefix}Signal";
        return sb.AppendIndentation(indentation)
                 .Append($"partial {keyword} {signalTypeDescriptor.Name} : global::{signalTypeName}<{signalTypeDescriptor.Name}>").AppendLine()
                 .AppendBlock(indentation);
    }

    private static StringBuilder AppendSignalTypesProperty(this StringBuilder sb,
                                                           Indentation indentation,
                                                           in TypeDescriptor signalTypeDescriptor)
    {
        return sb.AppendSignalTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append("public static ").AppendNewKeywordIfNecessary(signalTypeDescriptor).Append($"global::Conqueror.SignalTypes<{signalTypeDescriptor.Name}, IHandler> T => new();").AppendLine();
    }

    private static StringBuilder AppendSignalHandlerInterface(this StringBuilder sb,
                                                              Indentation indentation,
                                                              in TypeDescriptor signalTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendSignalTypeGeneratedCodeAttribute(indentation)
               .AppendIndentation(indentation)
               .Append("public ").AppendNewKeywordIfNecessary(signalTypeDescriptor).Append($"partial interface IHandler : global::Conqueror.ISignalHandler<{signalTypeDescriptor.Name}, IHandler, IHandler.Proxy>").AppendLine();

        using var d = sb.AppendBlock(indentation);

        return sb.AppendSignalTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"global::System.Threading.Tasks.Task Handle({signalTypeDescriptor.Name} signal, global::System.Threading.CancellationToken cancellationToken = default);").AppendLine()
                 .AppendLine()
                 .AppendSignalTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Threading.Tasks.Task global::Conqueror.ISignalHandler<{signalTypeDescriptor.Name}, IHandler>.Invoke(IHandler handler, {signalTypeDescriptor.Name} signal, global::System.Threading.CancellationToken cancellationToken)").AppendLineWithIndentation(indentation)
                 .AppendSingleIndent()
                 .Append("=> handler.Handle(signal, cancellationToken);").AppendLine()
                 .AppendLine()
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendSignalTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"public sealed class Proxy : global::Conqueror.SignalHandlerProxy<{signalTypeDescriptor.Name}, IHandler, Proxy>, IHandler;").AppendLine();
    }

    private static StringBuilder AppendTransportSignalHandlerInterface(this StringBuilder sb,
                                                                       Indentation indentation,
                                                                       in TypeDescriptor signalTypeDescriptor,
                                                                       in SignalAttributeDescriptor attributeDescriptor)
    {
        return sb.AppendIndentation(indentation)
                 .Append($"partial interface IHandler : global::{attributeDescriptor.Namespace}.I{attributeDescriptor.Prefix}SignalHandler<{signalTypeDescriptor.Name}, IHandler>;").AppendLine();
    }

    private static StringBuilder AppendSignalEmptyInstanceProperty(this StringBuilder sb,
                                                                   Indentation indentation,
                                                                   in TypeDescriptor signalTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendSignalTypeGeneratedCodeAttribute(indentation)
               .AppendIndentation(indentation);

        if (signalTypeDescriptor.HasProperties() || signalTypeDescriptor.IsAbstract)
        {
            return sb.Append($"static {signalTypeDescriptor.Name}? global::Conqueror.ISignal<{signalTypeDescriptor.Name}>.EmptyInstance => null;").AppendLine();
        }

        return sb.Append($"static {signalTypeDescriptor.Name} global::Conqueror.ISignal<{signalTypeDescriptor.Name}>.EmptyInstance => new();").AppendLine();
    }

    private static StringBuilder AppendAttributeParameterProperty(this StringBuilder sb,
                                                                  Indentation indentation,
                                                                  in TypeDescriptor signalTypeDescriptor,
                                                                  in SignalAttributeDescriptor attributeDescriptor,
                                                                  in AttributeParameterDescriptor parameterDescriptor)
    {
        var signalTypeName = attributeDescriptor.FullyQualifiedSignalTypeName ?? $"{attributeDescriptor.Namespace}.I{attributeDescriptor.Prefix}Signal";
        return sb.AppendSignalTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append("static ")
                 .AppendAttributeParameterPropertyType(in parameterDescriptor)
                 .Append($" global::{signalTypeName}<{signalTypeDescriptor.Name}>.{parameterDescriptor.Name} => ")
                 .AppendAttributeParameterValue(in parameterDescriptor.Value).Append(";").AppendLine();
    }

    private static StringBuilder AppendJsonSerializerContext(this StringBuilder sb,
                                                             Indentation indentation,
                                                             in TypeDescriptor signalTypeDescriptor,
                                                             bool hasJsonSerializerContext)
    {
        if (!hasJsonSerializerContext)
        {
            return sb;
        }

        return sb.AppendLine()
                 .AppendSignalTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Text.Json.Serialization.JsonSerializerContext global::Conqueror.ISignal<{signalTypeDescriptor.Name}>.JsonSerializerContext")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> global::{signalTypeDescriptor.FullyQualifiedName}JsonSerializerContext.Default;").AppendLine();
    }

    private static StringBuilder AppendPublicConstructorsProperty(this StringBuilder sb,
                                                                  Indentation indentation,
                                                                  in TypeDescriptor signalTypeDescriptor)
    {
        return sb.AppendLine()
                 .AppendSignalTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Collections.Generic.IEnumerable<global::System.Reflection.ConstructorInfo> global::Conqueror.ISignal<{signalTypeDescriptor.Name}>.PublicConstructors")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> typeof({signalTypeDescriptor.Name}).GetConstructors(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance);").AppendLine();
    }

    private static StringBuilder AppendPublicPropertiesProperty(this StringBuilder sb,
                                                                Indentation indentation,
                                                                in TypeDescriptor signalTypeDescriptor)
    {
        return sb.AppendLine()
                 .AppendSignalTypeGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Collections.Generic.IEnumerable<global::System.Reflection.PropertyInfo> global::Conqueror.ISignal<{signalTypeDescriptor.Name}>.PublicProperties")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> typeof({signalTypeDescriptor.Name}).GetProperties(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance);").AppendLine();
    }

    private static StringBuilder AppendNewKeywordIfNecessary(this StringBuilder sb,
                                                             in TypeDescriptor signalTypeDescriptor)
    {
        return signalTypeDescriptor.BaseTypes.Any(t => t.Attributes.Any(a => a.Attributes.Any(bt => bt.FullyQualifiedName == "Conqueror.Signalling.SignalTransportAttribute"))) ? sb.Append("new ") : sb;
    }

    private static StringBuilder AppendTransportSignalTypesProperties(this StringBuilder sb,
                                                                      Indentation indentation,
                                                                      in TypeDescriptor signalTypeDescriptor,
                                                                      in SignalAttributeDescriptor attributeDescriptor)
    {
        foreach (var property in attributeDescriptor.Properties)
        {
            _ = sb.AppendLine()
                  .AppendAttributeParameterProperty(indentation, in signalTypeDescriptor, in attributeDescriptor, in property);
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

    private static StringBuilder AppendSignalTypeGeneratedCodeAttribute(this StringBuilder sb,
                                                                        Indentation indentation)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = string.IsNullOrWhiteSpace(assemblyLocation) ? "unknown" : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return sb.AppendGeneratedCodeAttribute(indentation, typeof(SignalTypeGenerator).FullName ?? string.Empty, version);
    }
}
