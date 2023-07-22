using System;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

// can be made public once we have any configuration properties
internal static class InMemoryEventObserverTransportBuilderExtensions
{
    public static IEventObserverTransportBuilder UseInMemory(this IEventObserverTransportBuilder builder, Action<InMemoryEventObserverTransportConfiguration>? configure = null)
    {
        var configuration = new InMemoryEventObserverTransportConfiguration();
        configure?.Invoke(configuration);
        return builder.AddOrReplaceConfiguration(configuration);
    }
}

internal sealed class InMemoryEventObserverTransportConfiguration : IEventObserverTransportConfiguration
{
}
