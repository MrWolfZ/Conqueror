using Conqueror.SourceGenerators.Util.Signalling;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Signalling;

[Generator]
public sealed class SignallingAbstractionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeCoreSignallingAbstractionsGenerator("Conqueror.SignalAttribute");
    }
}
