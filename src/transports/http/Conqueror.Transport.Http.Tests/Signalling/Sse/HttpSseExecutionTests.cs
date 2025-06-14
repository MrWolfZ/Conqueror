using static Conqueror.Transport.Http.Tests.Signalling.HttpSignalConformityTestCase;
using static Conqueror.Transport.Http.Tests.Signalling.HttpSignalTestCases;

namespace Conqueror.Transport.Http.Tests.Signalling.Sse;

[TestFixture]
[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("Structure", "NUnit1028:The non-test method is public", Justification = "test case generation methods must be public")]
public sealed partial class HttpSseExecutionTests
{
    [Test]
    [Combinatorial]
    public void GivenMultipleHttpSseSignalTypesWithSameEventType_WhenRunningReceivers_ThrowsException([Values] bool runIndividually)
    {
        var clientServices = new ServiceCollection().AddConquerorHttpClient()
                                                    .AddSignalHandler<TestSignalWithDuplicateEventTypeHandler>();

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        Assert.That(
            () => runIndividually
                ? signalReceivers.RunHttpSseSignalReceiver<TestSignalWithDuplicateEventTypeHandler>(CancellationToken.None)
                : signalReceivers.RunHttpSseSignalReceivers(CancellationToken.None),
            Throws.InstanceOf<ReceiverExecutionFailedException>()
                  .With.InnerException.InstanceOf<InvalidOperationException>()
                  .With.InnerException.Message.Contains("is already used by signal type"));
    }

    [Test]
    [TestCaseSource(typeof(HttpSignalTestCases), nameof(CreateSimpleSuccessTestCases), [HttpSignalTransportType.Sse])]
    [SuppressMessage(
        "Structure",
        "NUnit1018:The number of parameters provided by the TestCaseSource does not match the number of parameters in the target method",
        Justification = "false positive")]
    public async Task GivenHttpSseSignalHandlerForMultipleSignalTypes_WhenRunningReceiver_OnlyConfiguresReceiverOnce(
        HttpSignalConformityExecutionSuccessTestCase testCase)
    {
        await using var host = await testCase.CreateTestHost();

        var httpClient = host.HttpClient;

        var configCount = 0;

        var clientServices = new ServiceCollection().AddConquerorHttpClient()
                                                    .AddSignalHandler<MultiTestSignalHandler>()
                                                    .AddSingleton<Action<IHttpSseSignalReceiver>>(r =>
                                                    {
                                                        configCount += 1;
                                                        _ = r.Enable(SseAddress)
                                                             .WithHttpClient(httpClient);
                                                    });

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var handle = signalReceivers.RunHttpSseSignalReceivers(cts.Token);

        await Assert.ThatAsync(
            () => handle.InitialConnectionTask.WaitAsync(host.AssertionTimeout, cts.Token),
            Throws.Nothing);

        Assert.That(configCount, Is.EqualTo(1));
    }

    [HttpSseSignal(EventType = "duplicate-event-type")]
    private sealed partial record TestSignalWithDuplicateEventType1(int Payload);

    [HttpSseSignal(EventType = "duplicate-event-type")]
    private sealed partial record TestSignalWithDuplicateEventType2(int Payload);

    private sealed partial class TestSignalWithDuplicateEventTypeHandler
        : TestSignalWithDuplicateEventType1.IHandler,
          TestSignalWithDuplicateEventType2.IHandler
    {
        public Task Handle(TestSignalWithDuplicateEventType1 signal, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        Task TestSignalWithDuplicateEventType2.IHandler.Handle(TestSignalWithDuplicateEventType2 signal, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.Enable(SseAddress);
    }
}
