using System;

namespace Conqueror;

public interface IQueryTransportClientBuilder
{
    IServiceProvider ServiceProvider { get; }

    Type QueryType { get; }

    Type ResponseType { get; }
}
