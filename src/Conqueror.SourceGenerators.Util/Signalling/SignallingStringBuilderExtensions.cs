using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Conqueror.SourceGenerators.Util.Signalling;

internal static class SignallingStringBuilderExtensions
{
    public static IDisposable AppendSignalType(this StringBuilder sb,
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

    public static StringBuilder AppendSignalTypesProperty(this StringBuilder sb,
                                                          Indentation indentation,
                                                          in TypeDescriptor signalTypeDescriptor)
    {
        return sb.AppendSignallingGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append("public static ").AppendNewKeywordIfNecessary(signalTypeDescriptor).Append($"global::Conqueror.SignalTypes<{signalTypeDescriptor.Name}, IHandler> T => global::Conqueror.SignalTypes<{signalTypeDescriptor.Name}, IHandler>.Default;").AppendLine();
    }

    public static StringBuilder AppendSignalHandlerInterface(this StringBuilder sb,
                                                             Indentation indentation,
                                                             in TypeDescriptor signalTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendSignallingGeneratedCodeAttribute(indentation)
               .AppendIndentation(indentation)
               .Append("public ").AppendNewKeywordIfNecessary(signalTypeDescriptor).Append($"partial interface IHandler : global::Conqueror.IGeneratedSignalHandler<{signalTypeDescriptor.Name}, IHandler>").AppendLine();

        using var d = sb.AppendBlock(indentation);

        return sb.AppendSignallingGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"global::System.Threading.Tasks.Task Handle({signalTypeDescriptor.Name} signal, global::System.Threading.CancellationToken cancellationToken = default);").AppendLine()
                 .AppendLine()
                 .AppendSignallingGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Threading.Tasks.Task global::Conqueror.IGeneratedSignalHandler<{signalTypeDescriptor.Name}, IHandler>.Invoke(IHandler handler, {signalTypeDescriptor.Name} signal, global::System.Threading.CancellationToken cancellationToken)").AppendLineWithIndentation(indentation)
                 .AppendSingleIndent()
                 .Append("=> handler.Handle(signal, cancellationToken);").AppendLine()
                 .AppendLine()
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendSignallingGeneratedCodeAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"public sealed class Adapter : global::Conqueror.GeneratedSignalHandlerAdapter<{signalTypeDescriptor.Name}, IHandler, Adapter>, IHandler;").AppendLine();
    }

    public static StringBuilder AppendSignalEmptyInstanceProperty(this StringBuilder sb,
                                                                  Indentation indentation,
                                                                  in TypeDescriptor signalTypeDescriptor)
    {
        sb = sb.AppendLine()
               .AppendSignallingGeneratedCodeAttribute(indentation)
               .AppendEditorBrowsableNeverAttribute(indentation)
               .AppendIndentation(indentation);

        if (signalTypeDescriptor.HasProperties())
        {
            return sb.Append($"static {signalTypeDescriptor.Name}? global::Conqueror.ISignal<{signalTypeDescriptor.Name}>.EmptyInstance => null;").AppendLine();
        }

        return sb.Append($"static {signalTypeDescriptor.Name} global::Conqueror.ISignal<{signalTypeDescriptor.Name}>.EmptyInstance => new();").AppendLine();
    }

    public static StringBuilder AppendSignalDefaultTypeInjector(this StringBuilder sb,
                                                                Indentation indentation,
                                                                in TypeDescriptor signalTypeDescriptor)
    {
        return sb.AppendLine()
                 .AppendSignallingGeneratedCodeAttribute(indentation)
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::Conqueror.IDefaultSignalTypesInjector global::Conqueror.ISignal<{signalTypeDescriptor.Name}>.DefaultTypeInjector")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent()
                 .Append($"=> global::Conqueror.DefaultSignalTypesInjector<{signalTypeDescriptor.Name}, IHandler, IHandler.Adapter>.Default;").AppendLine();
    }

    public static StringBuilder AppendSignalTypeInjectors(this StringBuilder sb,
                                                          Indentation indentation,
                                                          in TypeDescriptor signalTypeDescriptor)
    {
        return sb.AppendLine()
                 .AppendSignallingGeneratedCodeAttribute(indentation)
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Collections.Generic.IReadOnlyCollection<global::Conqueror.ISignalTypesInjector> global::Conqueror.ISignal<{signalTypeDescriptor.Name}>.TypeInjectors")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> global::Conqueror.ISignalTypesInjector.GetTypeInjectorsForSignalType<{signalTypeDescriptor.Name}>();").AppendLine();
    }

    public static StringBuilder AppendJsonSerializerContext(this StringBuilder sb,
                                                            Indentation indentation,
                                                            in TypeDescriptor signalTypeDescriptor,
                                                            bool hasJsonSerializerContext)
    {
        if (!hasJsonSerializerContext)
        {
            return sb;
        }

        return sb.AppendLine()
                 .AppendSignallingGeneratedCodeAttribute(indentation)
                 .AppendEditorBrowsableNeverAttribute(indentation)
                 .AppendIndentation(indentation)
                 .Append($"static global::System.Text.Json.Serialization.JsonSerializerContext global::Conqueror.ISignal<{signalTypeDescriptor.Name}>.JsonSerializerContext")
                 .AppendLineWithIndentation(indentation)
                 .AppendSingleIndent().Append($"=> global::{signalTypeDescriptor.FullyQualifiedName}JsonSerializerContext.Default;").AppendLine();
    }

    private static StringBuilder AppendNewKeywordIfNecessary(this StringBuilder sb,
                                                             in TypeDescriptor signalTypeDescriptor)
    {
        return signalTypeDescriptor.BaseTypes.Any(t => t.Attributes.Any(a => a.BaseTypes.Any(bt => bt.FullyQualifiedName == "Conqueror.Signalling.ConquerorSignalTransportAttribute"))) ? sb.Append("new ") : sb;
    }

    private static StringBuilder AppendSignallingGeneratedCodeAttribute(this StringBuilder sb,
                                                                        Indentation indentation)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = string.IsNullOrWhiteSpace(assemblyLocation) ? "unknown" : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return sb.AppendGeneratedCodeAttribute(indentation, "Conqueror.SourceGenerators.Signalling.SignallingAbstractionsGenerator", version);
    }
}
