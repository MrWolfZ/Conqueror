namespace Conqueror.CQS.CommandHandling;

public static class InProcessCommandTransportTypeExtensions
{
    public const string TransportName = "in-process";

    public static bool IsInProcess(this CommandTransportType transportClientType) => transportClientType.Name == TransportName;
}
