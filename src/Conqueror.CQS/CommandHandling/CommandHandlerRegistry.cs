using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandHandlerRegistry
    {
        private readonly IReadOnlyDictionary<(Type CommandType, Type? ResponseType), CommandHandlerMetadata> metadataLookup;

        public CommandHandlerRegistry(IEnumerable<CommandHandlerMetadata> metadata)
        {
            metadataLookup = metadata.ToDictionary(m => (m.CommandType, m.ResponseType));
        }

        public (ICommandHandler<TCommand, TResponse> Handler, CommandHandlerMetadata Metadata) GetCommandHandler<TCommand, TResponse>(IServiceProvider serviceProvider)
            where TCommand : class
        {
            if (!metadataLookup.TryGetValue((typeof(TCommand), typeof(TResponse)), out var metadata))
            {
                throw new ArgumentException($"there is no registered command handler for command type {typeof(TCommand).Name} and response type {typeof(TResponse).Name}");
            }

            var handler = (ICommandHandler<TCommand, TResponse>)serviceProvider.GetRequiredService(metadata.HandlerType);
            return (handler, metadata);
        }

        public (ICommandHandler<TCommand> Handler, CommandHandlerMetadata Metadata) GetCommandHandler<TCommand>(IServiceProvider serviceProvider)
            where TCommand : class
        {
            if (!metadataLookup.TryGetValue((typeof(TCommand), null), out var metadata))
            {
                throw new ArgumentException($"there is no registered command handler for command type {typeof(TCommand).Name}");
            }

            var handler = (ICommandHandler<TCommand>)serviceProvider.GetRequiredService(metadata.HandlerType);
            return (handler, metadata);
        }
    }
}
