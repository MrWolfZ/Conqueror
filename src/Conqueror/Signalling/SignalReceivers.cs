using System;

namespace Conqueror.Signalling;

internal sealed class SignalReceivers(IServiceProvider serviceProvider) : ISignalReceivers
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
}
