using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandHandlerRegistry : ICommandHandlerRegistry
{
    private readonly IReadOnlyCollection<CommandHandlerRegistration> registrations;

    public CommandHandlerRegistry(IEnumerable<CommandHandlerRegistration> registrations)
    {
        this.registrations = registrations.ToList();
    }

    public IReadOnlyCollection<CommandHandlerRegistration> GetCommandHandlerRegistrations() => registrations;
}
