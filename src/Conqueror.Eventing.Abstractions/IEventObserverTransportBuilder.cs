using System;

namespace Conqueror;

public interface IEventObserverTransportBuilder
{
    IServiceProvider ServiceProvider { get; }

    IEventObserverTransportBuilder AddOrReplaceConfiguration<TConfiguration>(TConfiguration configuration)
        where TConfiguration : class, IEventObserverTransportConfiguration;
}

public interface IEventObserverTransportConfiguration
{
}
