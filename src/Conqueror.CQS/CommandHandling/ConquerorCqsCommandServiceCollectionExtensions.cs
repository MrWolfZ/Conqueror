using System;
using System.Linq;
using Conqueror;
using Conqueror.CQS.CommandHandling;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsCommandServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureCommandPipeline<TCommandHandler>(this IServiceCollection services, Action<ICommandPipelineBuilder> configure)
            where TCommandHandler : ICommandHandler =>
            services.ConfigureCommandPipeline(typeof(TCommandHandler), configure);

        internal static IServiceCollection ConfigureCommandPipeline(this IServiceCollection services, Type handlerType, Action<ICommandPipelineBuilder> configure)
        {
            var existingConfiguration = services.FirstOrDefault(d => d.ImplementationInstance is CommandHandlerPipelineConfiguration c && c.HandlerType == handlerType);

            if (existingConfiguration is not null)
            {
                services.Remove(existingConfiguration);
            }

            services.AddSingleton(new CommandHandlerPipelineConfiguration(handlerType, configure));

            return services;
        }
    }
}
