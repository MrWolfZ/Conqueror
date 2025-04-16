using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using Conqueror.SourceGenerators.Util;
using Conqueror.SourceGenerators.Util.Messaging;

namespace Conqueror.Transport.Http.SourceGenerators.Messaging;

[SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "this is common for working with string builders")]
public static class HttpMessageAbstractionsSources
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
                 .AppendApiGroupNameProperty(indentation, in descriptor)
                 .AppendMessageSerializer(indentation, in descriptor);
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
            sb = sb.Append($", global::{responseTypeDescriptor.FullyQualifiedName}");
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
                 .Append($"=> global::Conqueror.HttpMessageTypesInjector<{messageTypeDescriptor.Name}, global::{responseTypeDescriptor.FullyQualifiedName}>.Default;").AppendLine();
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

    // TODO: nested properties
    // TODO: ignore if property is already defined to allow custom user code
    private static StringBuilder AppendMessageSerializer(this StringBuilder sb, Indentation indentation, in HttpMessageTypesDescriptor descriptor)
    {
        // only support query strings for GET messages; this also helps prevent
        // bugs in the generator for complex types from affecting too many message
        // types, and in case there is a bug for a complex GET message, the user can
        // always implement their own query string parsing
        if (descriptor.HttpMethod is not "GET")
        {
            return sb;
        }

        var messageTypeName = descriptor.MessageTypesDescriptor.MessageTypeDescriptor.Name;
        var fullyQualifiedResponseTypeName = descriptor.MessageTypesDescriptor.ResponseTypeDescriptor.FullyQualifiedName;

        sb.AppendLine()
          .AppendHttpMessageGeneratedCodeAttribute(indentation)
          .AppendIndentation(indentation)
          .Append($"static global::Conqueror.IHttpMessageSerializer<{messageTypeName}, global::{fullyQualifiedResponseTypeName}>? global::Conqueror.IHttpMessage<{messageTypeName}, global::{fullyQualifiedResponseTypeName}>.HttpMessageSerializer").AppendLineWithIndentation(indentation)
          .AppendSingleIndent()
          .Append($"=> new global::Conqueror.HttpMessageQueryStringSerializer<{messageTypeName}, global::{fullyQualifiedResponseTypeName}>(").AppendLine();

        using var d = indentation.Indent();
        using var d2 = indentation.Indent();

        return sb.AppendFromQueryStringFn(indentation, in descriptor.MessageTypesDescriptor.MessageTypeDescriptor)
                 .Append(',').AppendLine()
                 .GenerateToQueryStringFn(indentation, in descriptor.MessageTypesDescriptor.MessageTypeDescriptor)
                 .Append(");").AppendLine();
    }

    private static StringBuilder AppendFromQueryStringFn(this StringBuilder sb, Indentation indentation, in TypeDescriptor typeDescriptor)
    {
        // TODO: primary constructors support

        sb.AppendIndentation(indentation)
          .Append("query =>").AppendLine();

        using var d = sb.AppendBlock(indentation, addNewLineAtEnd: false);

        sb.AppendIndentation(indentation)
          .Append("if (query is null)").AppendLine();

        using (sb.AppendBlock(indentation))
        {
            sb.AppendIndentation(indentation)
              .Append("throw new global::System.ArgumentException(\"query must not be null\", nameof(query));").AppendLine();
        }

        sb.AppendLineWithIndentation(indentation);

        // Object initialization
        sb.Append($"var result = new {typeDescriptor.Name}").AppendLine();

        var seenArray = false;

        var unsupportedProperties = new List<string>();

        using (sb.AppendBlock(indentation, addNewLineAtEnd: false))
        {
            var isFirstProperty = true;

            foreach (var (propertyName, fullyQualifiedPropTypeName, isPrimitive, _, isString, enumerableDescriptor) in typeDescriptor.Properties)
            {
                var camelPropName = Uncapitalize(propertyName);

                if (!isFirstProperty)
                {
                    sb.AppendLine();
                }

                sb.AppendIndentation(indentation).Append(propertyName).Append(" = ");

                if (isPrimitive)
                {
                    // Handle primitive values
                    sb.Append($"query.TryGetValue(\"{camelPropName}\", out var {camelPropName}Values) && {camelPropName}Values.Count > 0 && {camelPropName}Values[0] is {{ }} {camelPropName}Value ? ")
                      .Append($"({fullyQualifiedPropTypeName})global::System.Convert.ChangeType({camelPropName}Value, typeof({fullyQualifiedPropTypeName})) : {(isString ? "string.Empty" : "default")}");
                }
                else if (enumerableDescriptor is { } enumerable)
                {
                    var itemPropertyTypeName = enumerable.FullyQualifiedItemTypeName;

                    if (!enumerable.ItemTypeIsPrimitive)
                    {
                        itemPropertyTypeName = "global::" + itemPropertyTypeName;
                    }

                    seenArray = seenArray || enumerable.IsArray;

                    // Handle enumerable values
                    sb.Append($"query.TryGetValue(\"{camelPropName}\", out var {camelPropName}Values) ? ");

                    if (enumerable.ItemTypeIsPrimitive)
                    {
                        if (!enumerable.IsArray)
                        {
                            sb.Append("[..");
                        }

                        sb.Append($"global::System.Array.ConvertAll(ToArray({camelPropName}Values), v => ({itemPropertyTypeName})global::System.Convert.ChangeType(v, typeof({itemPropertyTypeName}))!)");

                        if (!enumerable.IsArray)
                        {
                            sb.Append("]");
                        }
                    }
                    else
                    {
                        unsupportedProperties.Add(propertyName);
                        sb.Append("[]");
                    }

                    sb.Append(" : []");
                }
                else
                {
                    unsupportedProperties.Add(propertyName);
                    sb.Append("default!");
                }

                sb.AppendLine(",");

                isFirstProperty = false;
            }
        }

        sb = sb.Append(";").AppendLine();

        if (unsupportedProperties.Count > 0)
        {
            sb.AppendLineWithIndentation(indentation)
              .Append($"throw new global::System.NotSupportedException($\"type '{{typeof({typeDescriptor.Name})}}' contains unsupported properties: {string.Join(", ", unsupportedProperties)}\");").AppendLine();
        }
        else
        {
            sb.AppendLineWithIndentation(indentation)
              .Append("return result;").AppendLine();
        }

        if (seenArray)
        {
            sb.AppendLineWithIndentation(indentation)
              .Append("static T[] ToArray<T>(global::System.Collections.Generic.IEnumerable<T> source)").AppendLine();

            using (sb.AppendBlock(indentation))
            {
                sb.AppendIndentation(indentation)
                  .Append("return source as T[] ?? global::System.Linq.Enumerable.ToArray(source);").AppendLine();
            }
        }

        return sb;
    }

    private static StringBuilder GenerateToQueryStringFn(this StringBuilder sb, Indentation indentation, in TypeDescriptor typeDescriptor)
    {
        sb.AppendIndentation(indentation)
          .Append("message =>").AppendLine();

        using var d = sb.AppendBlock(indentation, addNewLineAtEnd: false);

        sb.AppendIndentation(indentation)
          .Append("global::System.Text.StringBuilder queryBuilder = new global::System.Text.StringBuilder();").AppendLine();

        var isFirst = true;
        var seenFirstProperty = false;
        var firstPropertyWasEnumerable = false;

        var unsupportedProperties = new List<string>();

        foreach (var property in typeDescriptor.Properties)
        {
            var objAccess = "message." + property.Name;

            if (property.IsPrimitive)
            {
                var separator = seenFirstProperty
                    ? firstPropertyWasEnumerable
                        ? "queryBuilder.Length == 0 ? '?' : '&'"
                        : "'&'"
                    : "'?'";

                sb.AppendLineWithIndentation(indentation)
                  .Append($"queryBuilder.Append({separator});").AppendLineWithIndentation(indentation)
                  .Append($"queryBuilder.Append(\"{Uncapitalize(property.Name)}=\");").AppendLineWithIndentation(indentation)
                  .Append($"queryBuilder.Append(global::System.Uri.EscapeDataString({objAccess}{(property.IsNullable ? "?" : string.Empty)}.ToString() ?? string.Empty));").AppendLine();

                seenFirstProperty = true;
                continue;
            }

            if (property.Enumerable is { } enumerable)
            {
                firstPropertyWasEnumerable = isFirst;

                sb.AppendLineWithIndentation(indentation)
                  .Append($"if ({objAccess} is not null)").AppendLine();

                using (sb.AppendBlock(indentation))
                {
                    sb.AppendIndentation(indentation)
                      .Append($"foreach (var item in {objAccess})").AppendLine();

                    using (sb.AppendBlock(indentation))
                    using (enumerable.IsNullable ? sb.AppendIndentation(indentation).Append("if (item is not null)").AppendLine().AppendBlock(indentation) : null)
                    {
                        var separator = seenFirstProperty ? "\"&\"" : "queryBuilder.Length == 0 ? \"?\" : \"&\"";

                        sb.AppendIndentation(indentation)
                          .Append($"queryBuilder.Append({separator});").AppendLineWithIndentation(indentation)
                          .Append($"queryBuilder.Append(\"{Uncapitalize(property.Name)}=\");").AppendLineWithIndentation(indentation)
                          .Append($"queryBuilder.Append(global::System.Uri.EscapeDataString(item{(enumerable.IsNullable ? "?" : string.Empty)}.ToString() ?? string.Empty));").AppendLine();
                    }
                }
            }
            else
            {
                unsupportedProperties.Add(property.Name);
            }

            isFirst = false;
        }

        if (unsupportedProperties.Count > 0)
        {
            return sb.AppendLineWithIndentation(indentation)
                    .Append($"throw new global::System.NotSupportedException($\"type '{{typeof({typeDescriptor.Name})}}' contains unsupported properties: {string.Join(", ", unsupportedProperties)}\");").AppendLine();
        }

        return sb.AppendLineWithIndentation(indentation)
                 .Append("return queryBuilder.ToString();").AppendLine();
    }

    private static StringBuilder AppendHttpMessageGeneratedCodeAttribute(this StringBuilder sb,
                                                                         Indentation indentation)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = string.IsNullOrWhiteSpace(assemblyLocation) ? "unknown" : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return sb.AppendGeneratedCodeAttribute(indentation, typeof(HttpMessageAbstractionsGenerator).FullName ?? string.Empty, version);
    }

    private static string Uncapitalize(string str)
        => char.ToLower(str[0], CultureInfo.InvariantCulture) + str.Substring(1);
}
