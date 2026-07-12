namespace Conqueror;

public sealed class IteratorFailedDueToInvalidFormattedConquerorContextDataException : IteratorFailedException
{
    public IteratorFailedDueToInvalidFormattedConquerorContextDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public IteratorFailedDueToInvalidFormattedConquerorContextDataException(string message)
        : base(message)
    {
    }

    private IteratorFailedDueToInvalidFormattedConquerorContextDataException() { }

    public override string WellKnownReason => WellKnownReasons.InvalidFormattedContextData;
}
