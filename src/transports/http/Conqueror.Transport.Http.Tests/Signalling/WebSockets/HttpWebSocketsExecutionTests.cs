using static Conqueror.Transport.Http.Tests.Signalling.HttpSignalConformityTestCase;
using static Conqueror.Transport.Http.Tests.Signalling.HttpSignalTestCases;

namespace Conqueror.Transport.Http.Tests.Signalling.WebSockets;

[TestFixture]
[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("Structure", "NUnit1028:The non-test method is public", Justification = "test case generation methods must be public")]
public sealed partial class HttpWebSocketsExecutionTests
{
    [Test]
    [Combinatorial]
    public void GivenMultipleHttpWebSocketsSignalTypesWithSameTag_WhenRunningReceivers_ThrowsException([Values] bool runIndividually)
    {
        var clientServices = new ServiceCollection().AddConquerorHttpClient()
                                                    .AddSignalHandler<TestSignalWithDuplicateTagHandler>();

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        Assert.That(
            () => runIndividually
                ? signalReceivers.RunHttpWebSocketsSignalReceiver<TestSignalWithDuplicateTagHandler>(CancellationToken.None)
                : signalReceivers.RunHttpWebSocketsSignalReceivers(CancellationToken.None),
            Throws.InstanceOf<SignalReceiverRunFailedException>()
                  .With.InnerException.InstanceOf<InvalidOperationException>()
                  .With.InnerException.Message.Contains("is already used by signal type"));
    }

    [Test]
    [TestCaseSource(typeof(HttpSignalTestCases), nameof(CreateSimpleSuccessTestCases), [HttpSignalTransportType.WebSockets])]
    [SuppressMessage(
        "Structure",
        "NUnit1018:The number of parameters provided by the TestCaseSource does not match the number of parameters in the target method",
        Justification = "false positive")]
    public async Task GivenHttpWebSocketsSignalHandlerForMultipleSignalTypes_WhenRunningReceiver_OnlyConfiguresReceiverOnce(
        HttpSignalConformityExecutionSuccessTestCase testCase)
    {
        await using var host = await testCase.CreateTestHost();

        var configCount = 0;

        var clientServices = new ServiceCollection().AddConquerorHttpClient()
                                                    .AddSignalHandler<MultiTestSignalHandler>()
                                                    .AddSingleton<Action<IHttpWebSocketsSignalReceiver>>(r =>
                                                    {
                                                        configCount += 1;
                                                        _ = r.Enable(WebSocketsAddress)
                                                             .WithWebSocketFactory(async (address, _)
                                                                                       //// ReSharper disable once AccessToDisposedClosure
                                                                                       => await host.ConnectToWebSocket(address));
                                                    });

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var run = signalReceivers.RunHttpWebSocketsSignalReceivers(cts.Token);

        await Assert.ThatAsync(
            () => run.InitialConnectionTask.WaitAsync(host.AssertionTimeout, cts.Token),
            Throws.Nothing);

        Assert.That(configCount, Is.EqualTo(1));
    }

    [HttpWebSocketsSignal(Tag = "duplicate-tag")]
    private sealed partial record TestSignalWithDuplicateTag1(int Payload);

    [HttpWebSocketsSignal(Tag = "duplicate-tag")]
    private sealed partial record TestSignalWithDuplicateTag2(int Payload);

    private sealed partial class TestSignalWithDuplicateTagHandler
        : TestSignalWithDuplicateTag1.IHandler,
          TestSignalWithDuplicateTag2.IHandler
    {
        public Task Handle(TestSignalWithDuplicateTag1 signal, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        Task TestSignalWithDuplicateTag2.IHandler.Handle(TestSignalWithDuplicateTag2 signal, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.Enable(WebSocketsAddress);
    }
}
