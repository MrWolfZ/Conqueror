using System.Collections.Generic;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandHandlerRegistry : ICommandHandlerRegistry
    {
        private readonly IReadOnlyCollection<CommandHandlerRegistration> registrations;

        public CommandHandlerRegistry(IReadOnlyCollection<CommandHandlerRegistration> registrations)
        {
            this.registrations = registrations;
        }

        public IReadOnlyCollection<CommandHandlerRegistration> GetCommandHandlerRegistrations() => registrations;
    }
}
