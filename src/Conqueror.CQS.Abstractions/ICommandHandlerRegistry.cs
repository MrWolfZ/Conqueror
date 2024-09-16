using System;
using System.Collections.Generic;

namespace Conqueror;

public interface ICommandHandlerRegistry
{
    IReadOnlyCollection<CommandHandlerRegistration> GetCommandHandlerRegistrations();

    CommandHandlerRegistration? GetCommandHandlerRegistration(Type commandType, Type? responseType);
}

public sealed record CommandHandlerRegistration(Type CommandType, Type? ResponseType, Type HandlerType, Delegate? ConfigurePipeline);
