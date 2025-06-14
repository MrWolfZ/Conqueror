using static Conqueror.Transport.Http.Tests.Signalling.HttpSignalTestCases;

namespace Conqueror.Transport.Http.Tests.Signalling.Sse;

[TestFixture]
public sealed class HttpSseExecutionConformityTests
    : SignalTransportExecutionConformityTests<
          HttpSseExecutionConformityTests,
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityExecutionSuccessTestCase,
          HttpSignalConformityExecutionErrorTestCase>,
      ISignalTransportExecutionConformityTests<
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityExecutionSuccessTestCase,
          HttpSignalConformityExecutionErrorTestCase>
{
    public static IEnumerable<HttpSignalConformityExecutionSuccessTestCase> CreateSuccessTestCases()
        => HttpSignalTestCases.CreateSuccessTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.Sse);

    public static IEnumerable<HttpSignalConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases()
        => HttpSignalTestCases.CreateSimpleSuccessTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.Sse);

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive")]
    public static IEnumerable<HttpSignalConformityExecutionErrorTestCase> CreateErrorTestCases()
        => HttpSignalTestCases.CreateErrorTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.Sse)
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
                                      PublishSignals = (_, _, _) => Task.CompletedTask,
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
                                      RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>()
                                                              .AddSignalHandler<MultiTestSignalHandler>(),
                                      PublishSignals = (_, _, _) => Task.CompletedTask,
                                      RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
                                  },
                              ]);

    public static IEnumerable<HttpSignalConformityExecutionErrorTestCase> CreateReconnectDelayTestCases()
        => HttpSignalTestCases.CreateReconnectDelayTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.Sse);
}
