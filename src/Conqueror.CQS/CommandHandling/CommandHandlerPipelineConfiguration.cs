using System;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandHandlerPipelineConfiguration
    {
        public CommandHandlerPipelineConfiguration(Type handlerType, Action<ICommandPipelineBuilder> configure)
        {
            HandlerType = handlerType;
            Configure = configure;
        }

        public Type HandlerType { get; }

        public Action<ICommandPipelineBuilder> Configure { get; }
    }
}
