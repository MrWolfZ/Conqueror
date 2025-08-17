namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class SignalTransportTypeHttpSseExtensions
{
    public static bool IsHttpServerSentEvents(this SignalTransportType transportType) =>
        string.Equals(
            transportType.Name,
            ConquerorTransportHttpConstants.ServersSentEventsTransportName,
            StringComparison.Ordinal
        );
}
