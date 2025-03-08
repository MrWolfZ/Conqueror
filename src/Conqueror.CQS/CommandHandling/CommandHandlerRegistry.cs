using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandHandlerRegistry : ICommandHandlerRegistry
{
    private readonly IReadOnlyCollection<CommandHandlerRegistration> allRegistrations;
    private readonly Dictionary<Type, CommandHandlerRegistrationInternal> registrationByCommandType;

    public CommandHandlerRegistry(IEnumerable<CommandHandlerRegistrationInternal> registrations)
    {
        registrationByCommandType = registrations.ToDictionary(r => r.CommandType);
        allRegistrations = registrationByCommandType.Values.Select(r => new CommandHandlerRegistration(r.CommandType, r.ResponseType, r.HandlerType)).ToList();
    }

    public IReadOnlyCollection<CommandHandlerRegistration> GetCommandHandlerRegistrations() => allRegistrations;

    public CommandHandlerRegistrationInternal? GetCommandHandlerRegistration(Type queryType)
    {
        return registrationByCommandType.GetValueOrDefault(queryType);
    }
}

public sealed record CommandHandlerRegistrationInternal(Type CommandType, Type? ResponseType, Type HandlerType, Delegate? ConfigurePipeline);
