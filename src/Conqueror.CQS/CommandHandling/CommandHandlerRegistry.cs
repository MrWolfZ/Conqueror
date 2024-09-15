using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandHandlerRegistry(IEnumerable<CommandHandlerRegistration> registrations) : ICommandHandlerRegistry
{
    private readonly IReadOnlyCollection<CommandHandlerRegistration> registrations = registrations.ToList();

    public IReadOnlyCollection<CommandHandlerRegistration> GetCommandHandlerRegistrations() => registrations;
}
