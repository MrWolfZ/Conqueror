using System;

namespace Conqueror
{
    public interface ICommandTransportBuilder
    {
        IServiceProvider ServiceProvider { get; }
    }
}
