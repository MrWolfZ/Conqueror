using System;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandTransportClientBuilder : ICommandTransportClientBuilder
    {
        public CommandTransportClientBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
    }
}
