using System;

namespace Conqueror
{
    public interface ICommandTransportClientBuilder
    {
        IServiceProvider ServiceProvider { get; }

        Type CommandType { get; }
    }
}
