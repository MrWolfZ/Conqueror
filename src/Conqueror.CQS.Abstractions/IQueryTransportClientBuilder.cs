using System;

namespace Conqueror;

public interface IQueryTransportClientBuilder
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }

    Type QueryType { get; }

    Type ResponseType { get; }
}
