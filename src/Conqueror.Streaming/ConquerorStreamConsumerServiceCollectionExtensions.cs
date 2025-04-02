using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Conqueror;
using Conqueror.Streaming;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamConsumerServiceCollectionExtensions
{
    private static readonly object NullKey = new();

    public static IServiceCollection AddConquerorStreamConsumer<TConsumer>(this IServiceCollection services,
                                                                           ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TConsumer : class, IStreamConsumer
    {
        return services.AddConquerorStreamConsumer(typeof(TConsumer), new(typeof(TConsumer), typeof(TConsumer), lifetime), null);
    }

    public static IServiceCollection AddConquerorStreamConsumer<TConsumer>(this IServiceCollection services,
                                                                           Func<IServiceProvider, TConsumer> factory,
                                                                           ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TConsumer : class, IStreamConsumer
    {
        return services.AddConquerorStreamConsumer(typeof(TConsumer), new(typeof(TConsumer), factory, lifetime), null);
    }

    public static IServiceCollection AddConquerorStreamConsumer<TConsumer>(this IServiceCollection services,
                                                                           TConsumer instance)
        where TConsumer : class, IStreamConsumer
    {
        return services.AddConquerorStreamConsumer(typeof(TConsumer), new(typeof(TConsumer), instance), null);
    }

    public static IServiceCollection AddConquerorStreamConsumerKeyed<TConsumer>(this IServiceCollection services,
                                                                                object key,
                                                                                ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TConsumer : class, IStreamConsumer
    {
        return services.AddConquerorStreamConsumer(typeof(TConsumer), new(typeof(TConsumer), key, typeof(TConsumer), lifetime), key);
    }

    public static IServiceCollection AddConquerorStreamConsumerKeyed<TConsumer>(this IServiceCollection services,
                                                                                object key,
                                                                                Func<IServiceProvider, object, TConsumer> factory,
                                                                                ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TConsumer : class, IStreamConsumer
    {
        return services.AddConquerorStreamConsumer(typeof(TConsumer), new(typeof(TConsumer), key, (p, k) => factory(p, k!), lifetime), key);
    }

    public static IServiceCollection AddConquerorStreamConsumerKeyed<TConsumer>(this IServiceCollection services,
                                                                                object key,
                                                                                TConsumer instance)
        where TConsumer : class, IStreamConsumer
    {
        return services.AddConquerorStreamConsumer(typeof(TConsumer), new(typeof(TConsumer), key, instance), key);
    }

    internal static IServiceCollection AddConquerorStreamConsumer(this IServiceCollection services,
                                                                  Type consumerType,
                                                                  ServiceDescriptor serviceDescriptor,
                                                                  object? key)
    {
        services.TryAdd(serviceDescriptor);
        return services.AddConquerorStreamConsumer(consumerType, key);
    }

    private static IServiceCollection AddConquerorStreamConsumer(this IServiceCollection services,
                                                                 Type consumerType,
                                                                 object? key)
    {
        consumerType.ValidateNoInvalidStreamConsumerInterface();

        services.AddConquerorStreaming();

        var addConsumerMethod = typeof(ConquerorStreamConsumerServiceCollectionExtensions).GetMethod(nameof(AddConsumer), BindingFlags.NonPublic | BindingFlags.Static);

        if (addConsumerMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddConsumer)}'");
        }

        var existingStreamConsumerRegistrations = (from s in services
                                                   where s.ServiceType.IsStreamConsumerConcreteType()
                                                   from t in s.ServiceType.GetStreamConsumerItemTypes()
                                                   let id = (s.ServiceKey ?? NullKey, t)
                                                   group s.ServiceType by id
                                                   into g
                                                   select (g.Key, g.ToHashSet())).ToDictionary();

        foreach (var itemType in consumerType.GetStreamConsumerItemTypes())
        {
            if (existingStreamConsumerRegistrations.TryGetValue((key ?? NullKey, itemType), out var existingConsumerTypes) && existingConsumerTypes.Any(t => t != consumerType))
            {
                throw new InvalidOperationException($"cannot add stream consumer type {consumerType} since a stream consumer{(key is not null ? $" with key {key}" : string.Empty)} type for item type {itemType} is already registered ({existingConsumerTypes.First()}); consider using keyed service registrations instead if you want multiple consumers for the same item type");
            }

            var genericAddConsumerMethod = addConsumerMethod.MakeGenericMethod(consumerType, itemType);

            try
            {
                _ = genericAddConsumerMethod.Invoke(null, [services, key]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static void AddConsumer<TConsumer, TItem>(this IServiceCollection services,
                                                      object? key)
        where TConsumer : class, IStreamConsumer
    {
        RegisterPlainInterface();
        RegisterCustomInterface();

        void RegisterPlainInterface()
        {
            services.Add(key is null
                             ? ServiceDescriptor.Transient<IStreamConsumer<TItem>>(p => CreateProxy(p, null))
                             : ServiceDescriptor.KeyedTransient<IStreamConsumer<TItem>>(key, CreateProxy));
        }

        StreamConsumerProxy<TItem> CreateProxy(IServiceProvider serviceProvider, object? k)
        {
            var consumerMiddlewareRegistry = serviceProvider.GetRequiredService<StreamConsumerMiddlewareRegistry>();
            var configurePipeline = CreatePipelineConfigurationFunction();
            return new(serviceProvider, configurePipeline, typeof(TConsumer), key, null, consumerMiddlewareRegistry);
        }

        void RegisterCustomInterface()
        {
            if (GetCustomStreamConsumerInterfaceType() is { } customInterfaceType)
            {
                var proxyType = ProxyTypeGenerator.Create(customInterfaceType, typeof(IStreamConsumer<TItem>), typeof(StreamConsumerGeneratedProxyBase<TItem>));

                if (key is null)
                {
                    services.TryAddTransient(customInterfaceType, proxyType);
                }
                else
                {
                    services.TryAddKeyedTransient(customInterfaceType, key, (p, k) => Activator.CreateInstance(proxyType, p.GetRequiredKeyedService(typeof(IStreamConsumer<TItem>), k))!);
                }
            }
        }

        static Type? GetCustomStreamConsumerInterfaceType()
        {
            var interfaces = typeof(TConsumer).GetInterfaces()
                                              .Concat([typeof(TConsumer)])
                                              .Where(i => i.IsCustomStreamConsumerInterfaceType<TItem>())
                                              .ToList();

            if (interfaces.Count < 1)
            {
                return null;
            }

            if (interfaces.Count > 1)
            {
                throw new InvalidOperationException($"stream consumer type {typeof(TConsumer)} implements more than one custom interface for item type {typeof(TItem)}");
            }

            var customConsumerInterface = interfaces.Single();

            if (customConsumerInterface.AllMethods().Count() > 1)
            {
                throw new ArgumentException($"stream consumer type {typeof(TConsumer)} implements custom interface {customConsumerInterface} that has extra methods; custom stream consumer interface types are not allowed to have any additional methods beside the {nameof(IStreamConsumer<object>.HandleItem)} method inherited from {typeof(IStreamConsumer<>).Name}");
            }

            return customConsumerInterface;
        }

        static Action<IStreamConsumerPipelineBuilder> CreatePipelineConfigurationFunction()
        {
            var pipelineConfigurationMethod = typeof(TConsumer).GetInterfaceMap(typeof(IStreamConsumer<TItem>)).TargetMethods.Single(m => m.Name == nameof(IStreamConsumer<TItem>.ConfigurePipeline));

            var builderParam = Expression.Parameter(typeof(IStreamConsumerPipelineBuilder));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<IStreamConsumerPipelineBuilder>)lambda;
        }
    }
}
