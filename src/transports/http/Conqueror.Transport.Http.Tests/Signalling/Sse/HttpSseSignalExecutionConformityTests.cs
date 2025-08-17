namespace Conqueror.Transport.Http.Tests.Signalling.Sse;

using static HttpSignalTestCases;

[TestFixture]
public sealed class HttpSseSignalExecutionConformityTests
    : SignalTransportExecutionConformityTests<
        HttpSseSignalExecutionConformityTests,
        HttpSignalTransportConformityTestHost,
        HttpSignalConformityExecutionSuccessTestCase,
        HttpSignalConformityExecutionErrorTestCase
    >,
        ISignalTransportExecutionConformityTests<
            HttpSignalTransportConformityTestHost,
            HttpSignalConformityExecutionSuccessTestCase,
            HttpSignalConformityExecutionErrorTestCase
        >
{
    public static bool TransportBuffersSignalsDuringReceiverDowntime => false;

    public static bool TransportSupportsReconnectingReceivers => true;

    public static bool TransportUsesCompetingConsumers => false;

    public static string TransportTypeName => ServersSentEventsTransportName;

    public static IEnumerable<HttpSignalConformityExecutionSuccessTestCase> CreateSuccessTestCases() =>
        HttpSignalTestCases.CreateSuccessTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.Sse);

    public static IEnumerable<HttpSignalConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases() =>
        HttpSignalTestCases.CreateSimpleSuccessTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.Sse);

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive")]
    [SuppressMessage(
        "Roslynator",
        "RCS1250:Use implicit/explicit object creation",
        Justification = "it is clear what objects are being created here"
    )]
    public static IEnumerable<HttpSignalConformityExecutionErrorTestCase> CreateErrorTestCases() =>
        HttpSignalTestCases
            .CreateErrorTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.Sse)
            .Concat(
                [
                    new()
                    {
                        Name = "single receiver with invalid server response type",
                        TransportType = HttpSignalConformityTestCase.HttpSignalTransportType.Sse,
                        ExpectedReceivedSignals = [],
                        ConfigurationExceptions = [],
                        PublishException = null,
                        ConnectionResponses = [(StatusCodes.Status200OK, ContentTypes.TextPlain, KeepAlive: true)],
                        ExpectedInitialConnectionCount = 1,
                        HandlerExceptions = [],
                        RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
                        PublishSignals = (_, _) => Task.CompletedTask,
                        RunReceivers = (r, ct) => r.RunHttpSseSignalReceiver<TestSignalHandler>(ct),
                    },
                    new()
                    {
                        Name = "multiple receivers with invalid server response type",
                        TransportType = HttpSignalConformityTestCase.HttpSignalTransportType.Sse,
                        ExpectedReceivedSignals = [],
                        ConfigurationExceptions = [],
                        PublishException = null,
                        ConnectionResponses =
                        [
                            (StatusCodes.Status200OK, ContentTypes.TextPlain, KeepAlive: true),
                            (StatusCodes.Status200OK, ContentTypes.TextPlain, KeepAlive: true),
                        ],
                        ExpectedInitialConnectionCount = 2,
                        NumOfReceivers = 2,
                        HandlerExceptions = [],
                        RegisterHandler = s =>
                            s.AddSignalHandler<TestSignalHandler>().AddSignalHandler<MultiTestSignalHandler>(),
                        PublishSignals = (_, _) => Task.CompletedTask,
                        RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
                    },
                ]
            );
}
