using Conqueror.SourceGenerators.Util.Eventing;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Eventing;

[Generator]
public sealed class EventingAbstractionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeCoreEventingAbstractionsGenerator("Conqueror.EventNotificationAttribute");
    }
}
