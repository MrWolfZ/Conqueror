using System;
using System.Collections.Generic;

namespace Conqueror
{
    public interface ICommandHandlerRegistry
    {
        public IReadOnlyCollection<CommandHandlerRegistration> GetCommandHandlerRegistrations();
    }
    
    public sealed record CommandHandlerRegistration(Type CommandType, Type? ResponseType, Type HandlerType);
}
