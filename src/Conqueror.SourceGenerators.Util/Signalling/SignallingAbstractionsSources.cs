using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Conqueror.SourceGenerators.Util.Signalling;

[SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "this is common for working with string builders")]
internal static class SignallingAbstractionsSources
{
    public static (string Content, string FileName) GenerateSignalTypes(SignalTypesDescriptor descriptor)
    {
        var sb = new StringBuilder();

        var content = sb.AppendGeneratedFile(in descriptor)
                        .ToString();

        sb.Clear();

        var filename = sb.Append(descriptor.SignalTypeDescriptor.FullyQualifiedName)
                         .Append("_SignalTypes.g.cs")
                         .Replace('<', '_')
                         .Replace('>', '_')
                         .Replace(',', '.')
                         .Replace(' ', '_')
                         .ToString();

        return new(content, filename);
    }

    private static StringBuilder AppendGeneratedFile(this StringBuilder sb, in SignalTypesDescriptor descriptor)
    {
        var signalTypeDescriptor = descriptor.SignalTypeDescriptor;

        var indentation = new Indentation();

        _ = sb.AppendFileHeader();

        using var ns = string.IsNullOrEmpty(signalTypeDescriptor.Namespace) ? null : sb.AppendNamespace(indentation, signalTypeDescriptor.Namespace);

        using var p = sb.AppendParentClasses(indentation, signalTypeDescriptor.ParentClasses);
        using var mt = sb.AppendSignalType(indentation, in signalTypeDescriptor);

        return sb.AppendSignalTypesProperty(indentation, in signalTypeDescriptor)
                 .AppendSignalHandlerInterface(indentation, in signalTypeDescriptor)
                 .AppendSignalEmptyInstanceProperty(indentation, in signalTypeDescriptor)
                 .AppendSignalDefaultTypeInjector(indentation, in signalTypeDescriptor)
                 .AppendSignalTypeInjectors(indentation, in signalTypeDescriptor)
                 .AppendJsonSerializerContext(indentation, in signalTypeDescriptor, descriptor.HasJsonSerializerContext);
    }
}
