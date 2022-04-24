using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandHandlerRegistry
    {
        private readonly IReadOnlyDictionary<(Type CommandType, Type? ResponseType), CommandHandlerMetadata> metadataLookup;

        public CommandHandlerRegistry(IEnumerable<CommandHandlerMetadata> metadata)
        {
            metadataLookup = metadata.ToDictionary(m => (m.CommandType, m.ResponseType));
        }

        public CommandHandlerMetadata GetCommandHandlerMetadata<TCommand, TResponse>()
            where TCommand : class
        {
            if (!metadataLookup.TryGetValue((typeof(TCommand), typeof(TResponse)), out var metadata))
            {
                throw new ArgumentException($"there is no registered command handler for command type {typeof(TCommand).Name} and response type {typeof(TResponse).Name}");
            }

            return metadata;
        }

        public CommandHandlerMetadata GetCommandHandlerMetadata<TCommand>()
            where TCommand : class
        {
            if (!metadataLookup.TryGetValue((typeof(TCommand), null), out var metadata))
            {
                throw new ArgumentException($"there is no registered command handler for command type {typeof(TCommand).Name}");
            }

            return metadata;
        }
    }
}
