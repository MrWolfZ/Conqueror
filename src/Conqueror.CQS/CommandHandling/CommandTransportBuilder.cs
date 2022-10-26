using System;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandTransportBuilder : ICommandTransportBuilder
    {
        public CommandTransportBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
    }
}
