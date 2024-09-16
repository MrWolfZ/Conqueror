using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandHandlerRegistry(IEnumerable<CommandHandlerRegistration> registrations) : ICommandHandlerRegistry
{
    private readonly Dictionary<(Type CommandType, Type? ResponseType), CommandHandlerRegistration> registrations
        = registrations.ToDictionary(r => (r.CommandType, r.ResponseType));

    public IReadOnlyCollection<CommandHandlerRegistration> GetCommandHandlerRegistrations() => registrations.Values.ToList();

    public CommandHandlerRegistration? GetCommandHandlerRegistration(Type commandType, Type? responseType)
    {
        return registrations.GetValueOrDefault((commandType, responseType));
    }
}
