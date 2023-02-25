using System;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandTransportClientBuilder : ICommandTransportClientBuilder
    {
        public CommandTransportClientBuilder(IServiceProvider serviceProvider, Type commandType)
        {
            ServiceProvider = serviceProvider;
            CommandType = commandType;
        }

        public IServiceProvider ServiceProvider { get; }

        public Type CommandType { get; }
    }
}
