using System;
using System.Collections.Generic;

namespace Conqueror;

public interface ICommandMiddlewareRegistry
{
    public IReadOnlyCollection<CommandMiddlewareRegistration> GetCommandMiddlewareRegistrations();
}

public sealed record CommandMiddlewareRegistration(Type MiddlewareType, Type? ConfigurationType);
