using System.Diagnostics;

namespace Conqueror.Signalling;

internal sealed class DefaultSignalIdFactory : ISignalIdFactory
{
    public string GenerateId() => ActivitySpanId.CreateRandom().ToString();
}
