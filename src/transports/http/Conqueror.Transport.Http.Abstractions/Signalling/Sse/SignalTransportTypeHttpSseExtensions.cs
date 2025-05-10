using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class SignalTransportTypeHttpSseExtensions
{
    public static bool IsHttpServerSentEvents(this SignalTransportType transportType)
        => transportType.Name == ConquerorTransportHttpConstants.ServersSentEventsTransportName;
}
