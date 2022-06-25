using System;

namespace Conqueror.CQS
{
    internal sealed class CommandHandlerMetadata
    {
        public CommandHandlerMetadata(Type commandType, Type? responseType, Type handlerType)
        {
            CommandType = commandType;
            ResponseType = responseType;
            HandlerType = handlerType;
        }

        public Type CommandType { get; }

        public Type? ResponseType { get; }

        public Type HandlerType { get; }
    }
}
