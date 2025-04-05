using System.Diagnostics;

namespace Conqueror.Messaging;

internal sealed class DefaultMessageIdFactory : IMessageIdFactory
{
    public string GenerateId() => ActivitySpanId.CreateRandom().ToString();
}
