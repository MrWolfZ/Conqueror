using Conqueror.SourceGenerators.Util.Messaging;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Messaging;

[Generator]
public sealed class MessagingAbstractionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeCoreMessageAbstractionsGenerator("Message", "Conqueror.MessageAttribute");
        context.InitializeCoreMessageAbstractionsGenerator("Message", "Conqueror.MessageAttribute`1");
    }
}
