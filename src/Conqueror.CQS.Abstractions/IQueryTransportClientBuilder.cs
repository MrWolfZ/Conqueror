using System;

namespace Conqueror
{
    public interface IQueryTransportClientBuilder
    {
        IServiceProvider ServiceProvider { get; }
    }
}
