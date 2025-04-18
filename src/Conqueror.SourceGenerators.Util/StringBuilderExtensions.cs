using System;
using System.Security.Cryptography;
using System.Text;

namespace Conqueror.SourceGenerators.Util;

public static class StringBuilderExtensions
{
    [ThreadStatic]
    private static SHA256? sha256;

    private static SHA256 Sha256 => sha256 ??= SHA256.Create();

    public static StringBuilder AppendSingleIndent(this StringBuilder sb)
    {
        return sb.Append("    ");
    }

    public static StringBuilder AppendIndentation(this StringBuilder sb, Indentation indentation)
    {
        for (var i = 0; i < indentation.Level; i++)
        {
            _ = sb.AppendSingleIndent();
        }

        return sb;
    }

    public static StringBuilder AppendLineWithIndentation(this StringBuilder sb, Indentation indentation)
    {
        return sb.AppendLine()
                 .AppendIndentation(indentation);
    }

    public static IDisposable AppendBlock(this StringBuilder sb, Indentation indentation, bool addNewLineAtEnd = true)
    {
        _ = sb.AppendIndentation(indentation)
              .Append("{")
              .AppendLine();

        var indentDisposable = indentation.Indent();
        return new AnonymousDisposable(() =>
        {
            indentDisposable.Dispose();

            _ = sb.AppendIndentation(indentation)
                  .Append("}");

            if (addNewLineAtEnd)
            {
                _ = sb.AppendLine();
            }
        });
    }

    public static IDisposable AppendNamespace(this StringBuilder sb,
                                              Indentation indentation,
                                              string namespaceName)
    {
        return sb.AppendIndentation(indentation).Append("namespace ").Append(namespaceName).AppendLine()
                 .AppendBlock(indentation);
    }

    public static IDisposable AppendParentClasses(this StringBuilder sb,
                                                  Indentation indentation,
                                                  EquatableArray<ParentClass> parentClasses)
    {
        var aggregateDisposable = new AggregateDisposable();

        foreach (var parentClass in parentClasses)
        {
            aggregateDisposable.Add(sb.AppendParentClass(indentation, parentClass));
        }

        return aggregateDisposable;
    }

    public static IDisposable AppendParentClass(this StringBuilder sb,
                                                Indentation indentation,
                                                ParentClass parentClass)
    {
        return sb.AppendIndentation(indentation).Append("partial ").Append(parentClass.Keyword).Append(' ').Append(parentClass.Name).AppendLine()
                 .AppendBlock(indentation);
    }

    public static StringBuilder AppendTypeNameWithInlinedTypeArguments(this StringBuilder sb,
                                                                       in TypeDescriptor typeDescriptor)
    {
        if (typeDescriptor.TypeArguments.Count == 0)
        {
            return sb.Append(typeDescriptor.SimpleName);
        }

        return sb.Append(typeDescriptor.SimpleName).Append("__").Append(string.Join("_", typeDescriptor.TypeArguments)).Append("__");
    }

    public static StringBuilder AppendTypeArguments(this StringBuilder sb,
                                                    in TypeDescriptor typeDescriptor)
    {
        return typeDescriptor.TypeArguments.Count == 0 ? sb : sb.Append($"<{string.Join(", ", typeDescriptor.TypeArguments)}>");
    }

    public static StringBuilder AppendConstraintClauses(this StringBuilder sb,
                                                        Indentation indentation,
                                                        in TypeDescriptor typeDescriptor)
    {
        return typeDescriptor.TypeConstraints is null
            ? sb
            : sb.AppendLineWithIndentation(indentation).Append(typeDescriptor.TypeConstraints.Replace(Environment.NewLine, Environment.NewLine + indentation));
    }

    public static StringBuilder AppendGeneratedCodeAttribute(this StringBuilder sb,
                                                             Indentation indentation,
                                                             string generatorName,
                                                             string generatorVersion)
    {
        return sb.AppendIndentation(indentation)
                 .Append($"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"{generatorName}\", \"{generatorVersion}\")]").AppendLine();
    }

    public static StringBuilder AppendEditorBrowsableNeverAttribute(this StringBuilder sb,
                                                                    Indentation indentation)
    {
        return sb.AppendIndentation(indentation)
                 .Append("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]").AppendLine();
    }

    public static StringBuilder AppendFileHeader(this StringBuilder sb)
    {
        const string header = """
                              //------------------------------------------------------------------------------
                              // <auto-generated>
                              //     This code was generated by a Conqueror source generator.
                              //
                              //     Changes to this file may cause incorrect behavior and will be lost if
                              //     the code is regenerated.
                              // </auto-generated>
                              //------------------------------------------------------------------------------

                              #nullable enable annotations
                              #nullable disable warnings

                              // Suppress warnings about [Obsolete] member usage in generated code.
                              #pragma warning disable CS0612, CS0618

                              """;

        return sb.AppendLine(header);
    }

    public static StringBuilder AppendHash(this StringBuilder sb, string text)
    {
        var uniqueMessageTypeId = Math.Abs(BitConverter.ToInt64(Sha256.ComputeHash(Encoding.UTF8.GetBytes(text)), 0));
        return sb.Append(uniqueMessageTypeId);
    }
}
