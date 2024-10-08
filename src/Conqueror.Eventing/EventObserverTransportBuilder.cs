﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Eventing;

internal sealed class EventObserverTransportBuilder(IServiceProvider serviceProvider) : IEventObserverTransportBuilder
{
    private readonly ConcurrentDictionary<Type, IEventObserverTransportConfiguration> configurationsByType = new();

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IEventObserverTransportBuilder AddOrReplaceConfiguration<TConfiguration>(TConfiguration configuration)
        where TConfiguration : class, IEventObserverTransportConfiguration
    {
        _ = configurationsByType.AddOrUpdate(typeof(TConfiguration), _ => configuration, (_, _) => configuration);
        return this;
    }

    public IReadOnlyCollection<IEventObserverTransportConfiguration> GetConfigurations()
    {
        return configurationsByType.Values.ToList();
    }
}
