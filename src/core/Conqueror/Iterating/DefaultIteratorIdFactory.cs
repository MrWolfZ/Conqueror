namespace Conqueror.Iterating;

internal sealed class DefaultIteratorIdFactory : IIteratorIdFactory
{
    public string GenerateId() => ActivitySpanId.CreateRandom().ToString();
}
