using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class CommandMiddlewareRegistry
    {
        private readonly IReadOnlyDictionary<Type, CommandMiddlewareMetadata> metadataLookup;

        public CommandMiddlewareRegistry(IEnumerable<CommandMiddlewareMetadata> metadata)
        {
            metadataLookup = metadata.ToDictionary(m => m.AttributeType);
        }

        public ICommandMiddleware<TConfiguration> GetMiddleware<TConfiguration>(IServiceProvider serviceProvider)
            where TConfiguration : CommandMiddlewareConfigurationAttribute
        {
            if (!metadataLookup.TryGetValue(typeof(TConfiguration), out var metadata))
            {
                throw new ArgumentException($"there is no registered command middleware for attribute {typeof(TConfiguration).Name}");
            }

            return (ICommandMiddleware<TConfiguration>)serviceProvider.GetRequiredService(metadata.MiddlewareType);
        }
    }
}
