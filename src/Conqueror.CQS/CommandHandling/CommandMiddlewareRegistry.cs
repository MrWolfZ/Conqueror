using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandMiddlewareRegistry : ICommandMiddlewareRegistry
    {
        private readonly IReadOnlyDictionary<Type, ICommandMiddlewareInvoker> invokers;
        private readonly IReadOnlyCollection<CommandMiddlewareRegistration> registrations;

        public CommandMiddlewareRegistry(IEnumerable<CommandMiddlewareRegistration> registrations, IEnumerable<ICommandMiddlewareInvoker> invokers)
        {
            this.registrations = registrations.ToList();
            this.invokers = invokers.ToDictionary(i => i.MiddlewareType);
        }

        public IReadOnlyCollection<CommandMiddlewareRegistration> GetCommandMiddlewareRegistrations() => registrations;

        internal IReadOnlyDictionary<Type, ICommandMiddlewareInvoker> GetCommandMiddlewareInvokers() => invokers;
    }
}
