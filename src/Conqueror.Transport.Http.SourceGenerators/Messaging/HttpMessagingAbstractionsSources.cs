using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using Conqueror.SourceGenerators.Util;

namespace Conqueror.Transport.Http.SourceGenerators.Messaging;

[SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "this is common for working with string builders")]
public static class HttpMessagingAbstractionsSources
{
    public static (string Content, string FileName) GenerateHttpMessageTypes(HttpMessageTypesDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendGeneratedFile(in descriptor)
                        .ToString();

        sb.Clear();

        var filename = sb.Append(descriptor.MessageTypesDescriptor.MessageTypeDescriptor.FullyQualifiedName)
                         .Append("_TransportHttpMessageTypes.g.cs")
                         .Replace('<', '_')
                         .Replace('>', '_')
                         .Replace(',', '.')
                         .Replace(' ', '_')
                         .ToString();

        return new(content, filename);
    }

    private static StringBuilder AppendGeneratedFile(this StringBuilder sb,
                                                     in HttpMessageTypesDescriptor descriptor)
    {
        var messageTypeDescriptor = descriptor.MessageTypesDescriptor.MessageTypeDescriptor;
        var responseTypeDescriptor = descriptor.MessageTypesDescriptor.ResponseTypeDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(messageTypeDescriptor.Namespace) ? null : sb.AppendNamespace(indentation, messageTypeDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, messageTypeDescriptor.ParentClasses);

        using var mt = sb.AppendHttpMessageType(indentation, in messageTypeDescriptor, in responseTypeDescriptor);

        return sb.AppendHttpMessageTypeInjector(indentation, in messageTypeDescriptor, in responseTypeDescriptor)
                 .AppendHttpMethodProperty(indentation, in descriptor)
                 .AppendPathPrefixProperty(indentation, in descriptor)
                 .AppendPathProperty(indentation, in descriptor)
                 .AppendFullPathProperty(indentation, in descriptor)
                 .AppendVersionProperty(indentation, in descriptor)
                 .AppendSuccessStatusCodeProperty(indentation, in descriptor)
                 .AppendNameProperty(indentation, in descriptor)
                 .AppendApiGroupNameProperty(indentation, in descriptor);
    }

    private static IDisposable AppendHttpMessageType(this StringBuilder sb,
                                                     Indentation indentation,
                                                     in TypeDescriptor messageTypeDescriptor,
                                                     in TypeDescriptor responseTypeDescriptor)
    {
        var keyword = messageTypeDescriptor.IsRecord ? "record" : "class";

        sb = sb.AppendIndentation(indentation)
               .Append("/// <summary>").AppendLineWithIndentation(indentation)
               .Append($"///     HTTP message Types for <see cref=\"global::{messageTypeDescriptor.FullyQualifiedName}\" />.").AppendLineWithIndentation(indentation)
               .Append("/// </summary>").AppendLineWithIndentation(indentation)
               .Append($"partial {keyword} {messageTypeDescriptor.Name} : global::Conqueror.IHttpMessage<{messageTypeDescriptor.Name}");

        if (!responseTypeDescriptor.IsUnitMessageResponse())
        {
            sb = sb.Append($", global::{responseTypeDescriptor.FullyQualifiedName()}");
        }

        return sb.Append(">").AppendLine()
                 .AppendBlock(indentation);
    }

    private static StringBuilder AppendHttpMessageTypeInjector(this StringBuilder sb,
                                                               Indentation indentation,
                                                               in TypeDescriptor messageTypeDescriptor,
                                                               in TypeDescriptor responseTypeDescriptor)
    {
        return sb.AppendHttpMessageGeneratedCodeAttribute(indentation)
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append("static global::Conqueror.IHttpMessageTypesInjector global::Conqueror.IHttpMessage.HttpMessageTypesInjector")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent()
                 .Append($"=> global::Conqueror.HttpMessageTypesInjector<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName()}>.Default;").AppendLine();
    }

    private static StringBuilder AppendHttpMethodProperty(this StringBuilder sb, Indentation indentation, in HttpMessageTypesDescriptor descriptor)
    {
        if (descriptor.HttpMethod is { } value)
        {
            sb.AppendLine()
              .AppendHttpMessageGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append($"public static string HttpMethod {{ get; }} = \"{value}\";").AppendLine();
        }

        return sb;
    }

    private static StringBuilder AppendPathPrefixProperty(this StringBuilder sb, Indentation indentation, in HttpMessageTypesDescriptor descriptor)
    {
        if (descriptor.PathPrefix is { } value)
        {
            sb.AppendLine()
              .AppendHttpMessageGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append($"public static string PathPrefix {{ get; }} = \"{value}\";").AppendLine();
        }

        return sb;
    }

    private static StringBuilder AppendPathProperty(this StringBuilder sb, Indentation indentation, in HttpMessageTypesDescriptor descriptor)
    {
        if (descriptor.Path is { } value)
        {
            sb.AppendLine()
              .AppendHttpMessageGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append($"public static string Path {{ get; }} = \"{value}\";").AppendLine();
        }

        return sb;
    }

    private static StringBuilder AppendFullPathProperty(this StringBuilder sb, Indentation indentation, in HttpMessageTypesDescriptor descriptor)
    {
        if (descriptor.FullPath is { } value)
        {
            sb.AppendLine()
              .AppendHttpMessageGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append($"public static string FullPath {{ get; }} = \"{value}\";").AppendLine();
        }

        return sb;
    }

    private static StringBuilder AppendVersionProperty(this StringBuilder sb, Indentation indentation, in HttpMessageTypesDescriptor descriptor)
    {
        if (descriptor.Version is { } value)
        {
            sb.AppendLine()
              .AppendHttpMessageGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append($"public static string Version {{ get; }} = \"{value}\";").AppendLine();
        }

        return sb;
    }

    private static StringBuilder AppendSuccessStatusCodeProperty(this StringBuilder sb, Indentation indentation, in HttpMessageTypesDescriptor descriptor)
    {
        if (descriptor.SuccessStatusCode is { } value)
        {
            sb.AppendLine()
              .AppendHttpMessageGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append($"public static int SuccessStatusCode {{ get; }} = {value};").AppendLine();
        }

        return sb;
    }

    private static StringBuilder AppendNameProperty(this StringBuilder sb, Indentation indentation, in HttpMessageTypesDescriptor descriptor)
    {
        if (descriptor.Name is { } value)
        {
            sb.AppendLine()
              .AppendHttpMessageGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append($"public static string Name {{ get; }} = \"{value}\";").AppendLine();
        }

        return sb;
    }

    private static StringBuilder AppendApiGroupNameProperty(this StringBuilder sb, Indentation indentation, in HttpMessageTypesDescriptor descriptor)
    {
        if (descriptor.ApiGroupName is { } value)
        {
            sb.AppendLine()
              .AppendHttpMessageGeneratedCodeAttribute(indentation)
              .AppendIndentation(indentation)
              .Append($"public static string ApiGroupName {{ get; }} = \"{value}\";").AppendLine();
        }

        return sb;
    }

    private static StringBuilder AppendHttpMessageGeneratedCodeAttribute(this StringBuilder sb,
                                                                         Indentation indentation)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = string.IsNullOrWhiteSpace(assemblyLocation) ? "unknown" : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return sb.AppendGeneratedCodeAttribute(indentation, typeof(HttpMessagingAbstractionsGenerator).FullName ?? string.Empty, version);
    }

    private static string Uncapitalize(string str)
        => char.ToLower(str[0], CultureInfo.InvariantCulture) + str.Substring(1);
}
