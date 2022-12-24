using System.Collections.Generic;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandMiddlewareRegistry : ICommandMiddlewareRegistry
    {
        private readonly IReadOnlyCollection<CommandMiddlewareRegistration> registrations;

        public CommandMiddlewareRegistry(IReadOnlyCollection<CommandMiddlewareRegistration> registrations)
        {
            this.registrations = registrations;
        }

        public IReadOnlyCollection<CommandMiddlewareRegistration> GetCommandMiddlewareRegistrations() => registrations;
    }
}
