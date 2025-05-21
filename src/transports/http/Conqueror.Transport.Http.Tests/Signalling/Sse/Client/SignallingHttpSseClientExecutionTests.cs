using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using static Conqueror.Transport.Http.Tests.Signalling.HttpTestSignals;

namespace Conqueror.Transport.Http.Tests.Signalling.Sse.Client;

[TestFixture]
[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
public sealed partial class SignallingHttpSseClientExecutionTests
{
    private const string AuthorizationHeaderScheme = "Basic";
    private const string AuthorizationHeaderValue = "username:password";
    private const string AuthorizationHeader = $"{AuthorizationHeaderScheme} {AuthorizationHeaderValue}";
    private const string TestHeaderValue = "test-value";

    private readonly ConcurrentQueue<(int StatusCode, string ContentType, bool KeepAlive)?> serverConnectionResponses = [];

    private int serverCallCount;
    private int serverResponseHasBegunCount;
    private int serverResponseHasFinishedCount;
    private IHeaderDictionary? receivedHeadersOnServer;
    private CancellationToken? serverCancellationToken;

    [Test]
    [TestCaseSource(typeof(HttpTestSignals), nameof(GenerateTestCaseData))]
    public async Task GivenTestHttpSseSignal_WhenRunningReceivers_ReceiversReceiveCorrectSignals(HttpSignalTestCase testCase)
    {
        await using var host = await CreateTestHost(
            testCase.RegisterServerServices,
            app => app.MapSignalEndpoints());

        var httpClient = host.HttpClient;

        var clientServices = testCase.RegisterClientServices(new ServiceCollection())
                                     .AddSingleton<FnToCallFromHandler>((s, p) =>
                                     {
                                         var observations = p.GetRequiredService<TestObservations>();
                                         observations.ReceivedSignals.Enqueue(s);

                                         return Task.CompletedTask;
                                     })
                                     .AddSingleton<Action<IHttpSseSignalReceiver>>(r => r.Enable(SseAddress)
                                                                                         .WithHttpClient(httpClient)

                                                                                         // test that headers are passed and function can be overwritten
                                                                                         .WithHeaders(h => h.Add("should-not-be-set", TestHeaderValue))
                                                                                         .WithHeaders(h =>
                                                                                         {
                                                                                             h.Authorization = new(
                                                                                                 "Basic",
                                                                                                 AuthorizationHeaderValue);
                                                                                             h.Add("test-header", TestHeaderValue);
                                                                                         }));

        if (testCase.ExpectedReceivedSignals.FirstOrDefault() is TestSignalWithCustomSerializedPayloadType)
        {
            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            jsonSerializerOptions.Converters.Add(new TestSignalWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory());
            jsonSerializerOptions.MakeReadOnly(true);

            _ = clientServices.AddSingleton(jsonSerializerOptions);
        }

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var run = testCase.RunReceiver(signalReceivers, cts.Token);

        // for test cases without any enabled signal types, the receivers should complete immediately
        if (string.IsNullOrWhiteSpace(testCase.QueryString))
        {
            var runTask = run.CompletionTask;
            Assert.That(
                () => runTask.IsCompletedSuccessfully,
                Is.True
                  .After(host.AssertionTimeoutInMs)
                  .MilliSeconds
                  .PollEvery(10)
                  .MilliSeconds);

            return;
        }

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(1)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        Assert.That(receivedHeadersOnServer!.Authorization.ToString(), Is.EqualTo(AuthorizationHeader));
        Assert.That(receivedHeadersOnServer, Does.ContainKey("test-header").WithValue("test-value"));

        await testCase.PublishSignals(host.Resolve<ISignalPublishers>());

        var observations = clientServiceProvider.GetRequiredService<TestObservations>();

        Assert.That(
            () => observations.ReceivedSignals,
            Is.EqualTo(testCase.ExpectedReceivedSignals)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        if (testCase.ExpectedReceivedSignals.FirstOrDefault() is TestSignalWithMiddleware)
        {
            var seenTransportTypeOnServer = host.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportTypeOnServer?.IsHttpServerSentEvents(), Is.True, $"transport type is {seenTransportTypeOnServer?.Name}");
            Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(SignalTransportRole.Publisher));

            var seenTransportTypeOnClient = clientServiceProvider.GetRequiredService<TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportTypeOnClient?.IsHttpServerSentEvents(), Is.True, $"transport type is {seenTransportTypeOnClient?.Name}");
            Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(SignalTransportRole.Receiver));
        }

        await cts.CancelAsync();
        await run.CompletionTask;
    }

    [Test]
    [Combinatorial]
    public async Task GivenMultipleHttpSseSignalTypesWithSameEventType_WhenRunningReceivers_ThrowsException(
        [Values] bool runIndividually)
    {
        await using var host = await CreateTestHost(
            services => services.AddConqueror()
                                .AddSingleton<TestObservations>()
                                .AddRouting(),
            app => app.MapSignalEndpoints());

        var clientServices = new ServiceCollection().AddSignalHandler<TestSignalWithDuplicateEventTypeHandler>();

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        var ct = host.TestTimeoutToken;
        Assert.That(
            () => runIndividually
                ? signalReceivers.RunHttpSseSignalReceiver<TestSignalWithDuplicateEventTypeHandler>(ct)
                : signalReceivers.RunHttpSseSignalReceivers(ct),
            Throws.InstanceOf<HttpSseSignalReceiverRunFailedException>()
                  .With.InnerException.InstanceOf<InvalidOperationException>()
                  .With.InnerException.Message.Contains("is already used by signal type"));
    }

    [Test]
    [Combinatorial]
    public async Task GivenHttpSseSignalHandler_WhenRunningReceiverMultipleTimes_SignalsAreReceivedMultipleTimes(
        [Values] bool runIndividually)
    {
        await using var host = await CreateTestHost(
            services => services.AddConqueror()
                                .AddSingleton<TestObservations>()
                                .AddRouting(),
            app => app.MapSignalEndpoints());

        var httpClient = host.HttpClient;
        var observations = new TestObservations();

        var clientServices = new ServiceCollection().AddSignalHandler<TestSignalHandler>()
                                                    .AddSingleton<FnToCallFromHandler>((s, _) =>
                                                    {
                                                        observations.ReceivedSignals.Enqueue(s);

                                                        return Task.CompletedTask;
                                                    })
                                                    .AddSingleton<Action<IHttpSseSignalReceiver>>(r => r.Enable(SseAddress)
                                                                                                        .WithHttpClient(httpClient));

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var run1 = runIndividually
            ? signalReceivers.RunHttpSseSignalReceiver<TestSignalHandler>(cts.Token)
            : signalReceivers.RunHttpSseSignalReceivers(cts.Token);
        await using var run2 = runIndividually
            ? signalReceivers.RunHttpSseSignalReceiver<TestSignalHandler>(cts.Token)
            : signalReceivers.RunHttpSseSignalReceivers(cts.Token);

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(2)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        var signal = new TestSignal { Payload = 10 };
        await host.Resolve<ISignalPublishers>()
                  .For(TestSignal.T)
                  .WithTransport(b => b.UseHttpServerSentEvents())
                  .Handle(signal, cts.Token);

        Assert.That(
            () => observations.ReceivedSignals,
            Is.EqualTo(new[] { signal, signal })
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);
    }

    [Test]
    [Combinatorial]
    public async Task GivenHttpSseSignalHandler_WhenRunningAndStoppingReceiverMultipleTimes_SignalsAreReceivedMultipleTimes(
        [Values] bool runIndividually)
    {
        await using var host = await CreateTestHost(
            services => services.AddConqueror()
                                .AddSingleton<TestObservations>()
                                .AddRouting(),
            app => app.MapSignalEndpoints());

        var httpClient = host.HttpClient;
        var observations = new TestObservations();

        var clientServices = new ServiceCollection().AddSignalHandler<TestSignalHandler>()
                                                    .AddSingleton<FnToCallFromHandler>((s, _) =>
                                                    {
                                                        observations.ReceivedSignals.Enqueue(s);

                                                        return Task.CompletedTask;
                                                    })
                                                    .AddSingleton<Action<IHttpSseSignalReceiver>>(r => r.Enable(SseAddress)
                                                                                                        .WithHttpClient(httpClient));

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        var signal1 = new TestSignal { Payload = 10 };
        var signal2 = new TestSignal { Payload = 20 };
        var signal3 = new TestSignal { Payload = 30 };

        using var cts1 = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var run1 = runIndividually
            ? signalReceivers.RunHttpSseSignalReceiver<TestSignalHandler>(cts1.Token)
            : signalReceivers.RunHttpSseSignalReceivers(cts1.Token);

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(1)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await host.Resolve<ISignalPublishers>()
                  .For(TestSignal.T)
                  .WithTransport(b => b.UseHttpServerSentEvents())
                  .Handle(signal1, cts1.Token);

        Assert.That(
            () => observations.ReceivedSignals,
            Is.EqualTo(new[] { signal1 })
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await cts1.CancelAsync();
        await run1.CompletionTask;

        await host.Resolve<ISignalPublishers>()
                  .For(TestSignal.T)
                  .WithTransport(b => b.UseHttpServerSentEvents())
                  .Handle(signal2, host.TestTimeoutToken);

        using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var run2 = runIndividually
            ? signalReceivers.RunHttpSseSignalReceiver<TestSignalHandler>(cts2.Token)
            : signalReceivers.RunHttpSseSignalReceivers(cts2.Token);

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(2)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await host.Resolve<ISignalPublishers>()
                  .For(TestSignal.T)
                  .WithTransport(b => b.UseHttpServerSentEvents())
                  .Handle(signal3, cts2.Token);

        Assert.That(
            () => observations.ReceivedSignals,
            Is.EqualTo(new[] { signal1, signal3 })
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);
    }

    [Test]
    [Combinatorial]
    public async Task GivenHttpSseSignalHandler_WhenCancellingReceiver_PerformsCleanShutdown(
        [Values] bool runIndividually,
        [Values] bool useCancel)
    {
        await using var host = await CreateTestHost(
            services => services.AddConqueror()
                                .AddSingleton<TestObservations>()
                                .AddRouting(),
            app => app.MapSignalEndpoints());

        var httpClient = host.HttpClient;

        var clientServices = new ServiceCollection().AddSignalHandler<TestSignalHandler>()
                                                    .AddSingleton<Action<IHttpSseSignalReceiver>>(r => r.Enable(SseAddress)
                                                                                                        .WithHttpClient(httpClient));

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var run = runIndividually
            ? signalReceivers.RunHttpSseSignalReceiver<TestSignalHandler>(cts.Token)
            : signalReceivers.RunHttpSseSignalReceivers(cts.Token);

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(1)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await host.Resolve<ISignalPublishers>()
                  .For(TestSignal.T)
                  .WithTransport(b => b.UseHttpServerSentEvents())
                  .Handle(new() { Payload = 10 }, cts.Token);

        if (useCancel)
        {
            await cts.CancelAsync();
        }
        else
        {
            // ReSharper disable once DisposeOnUsingVariable (testing this case explicitly)
            await run.DisposeAsync();
        }

        var completionTask = run.CompletionTask;
        Assert.That(
            () => completionTask.IsCompletedSuccessfully,
            Is.True
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);
    }

    [Test]
    [Combinatorial]
    public async Task GivenMultipleHttpSseSignalHandlers_WhenCancellingReceivers_PerformsCleanShutdown(
        [Values] bool useCancel)
    {
        await using var host = await CreateTestHost(
            services => services.AddConqueror()
                                .AddSingleton<TestObservations>()
                                .AddRouting(),
            app => app.MapSignalEndpoints());

        var httpClient = host.HttpClient;

        var clientServices = new ServiceCollection().AddSignalHandler<TestSignalHandler>()
                                                    .AddSignalHandler<MultiTestSignalHandler>()
                                                    .AddSingleton<Action<IHttpSseSignalReceiver>>(r => r.Enable(SseAddress)
                                                                                                        .WithHttpClient(httpClient));

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var run = signalReceivers.RunHttpSseSignalReceivers(cts.Token);

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(2)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await host.Resolve<ISignalPublishers>()
                  .For(TestSignal.T)
                  .WithTransport(b => b.UseHttpServerSentEvents())
                  .Handle(new() { Payload = 10 }, cts.Token);

        if (useCancel)
        {
            await cts.CancelAsync();
        }
        else
        {
            // ReSharper disable once DisposeOnUsingVariable (testing this case explicitly)
            await run.DisposeAsync();
        }

        var completionTask = run.CompletionTask;
        Assert.That(
            () => completionTask.IsCompletedSuccessfully,
            Is.True
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);
    }

    [Test]
    [TestCaseSource(nameof(GenerateErrorTestCaseData))]
    [Retry(3)] // there might be some flakiness due to timing issues
    public async Task GivenHttpSseSignalHandlers_WhenErrorsOccur_CorrectBehaviorIsExecuted(object testCaseParam)
    {
        var testCase = (HttpSseSignalErrorTestCase)testCaseParam; // cast instead of direct parameter type to keep the type private

        await using var host = await CreateTestHost(
            testCase.RegisterServerServices,
            app => app.MapSignalEndpoints());

        var httpClient = host.HttpClient;
        var configurationExceptions = new Queue<Exception?>(testCase.ConfigurationExceptions);

        using var serverCts = new CancellationTokenSource();
        serverCancellationToken = serverCts.Token;

        var clientServices = testCase.RegisterClientServices(new ServiceCollection())
                                     .AddSingleton(new ConcurrentQueue<Exception?>(testCase.HandlerExceptions))
                                     .AddSingleton<Action<IHttpSseSignalReceiver>>(r =>
                                     {
                                         if (configurationExceptions.TryDequeue(out var ex) && ex is not null)
                                         {
                                             throw ex;
                                         }

                                         _ = r.Enable(SseAddress).WithHttpClient(httpClient);
                                     });

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        var ct = cts.Token;

        var configurationException = testCase.ConfigurationExceptions.OfType<Exception>().FirstOrDefault();
        if (configurationException is not null)
        {
            Assert.That(
                () => testCase.RunReceivers(signalReceivers, ct),
                Throws.InstanceOf<HttpSseSignalReceiverRunFailedException>()
                      .With.InnerException.SameAs(configurationException));

            Assert.That(() => serverCallCount, Is.EqualTo(0));

            return;
        }

        foreach (var statusCode in testCase.ConnectionResponses)
        {
            serverConnectionResponses.Enqueue(statusCode);
        }

        await using var run = testCase.RunReceivers(signalReceivers, cts.Token);

        Assert.That(
            () => serverCallCount,
            Is.EqualTo(testCase.ExpectedInitialConnectionCount)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await Task.Delay(10, ct); // give the connections time to start properly

        var connectionUnrecoverableErrorCount = testCase.ConnectionResponses
                                                        .Count(r => r is not null
                                                                    && (r.Value.StatusCode is >= 400 and < 500
                                                                        || (r.Value.StatusCode is >= 200 and < 400
                                                                            && r.Value.ContentType != ContentTypes.EventStream)));

        if (connectionUnrecoverableErrorCount == 1)
        {
            await Assert.ThatAsync(
                async () => await await Task.WhenAny(run.CompletionTask, Task.Delay(host.AssertionTimeoutInMs, cts.Token)),
                Throws.InstanceOf<HttpSseSignalReceiverRunFailedException>());
        }

        if (connectionUnrecoverableErrorCount > 1)
        {
            await Assert.ThatAsync(
                async () => await await Task.WhenAny(run.CompletionTask, Task.Delay(host.AssertionTimeoutInMs, cts.Token)),
                Throws.InstanceOf<AggregateException>()
                      .With.Property("InnerExceptions")
                      .Count.EqualTo(connectionUnrecoverableErrorCount)
                      .With.Property("InnerExceptions")
                      .Matches<ReadOnlyCollection<Exception>>(exs => exs.All(ex => ex is HttpSseSignalReceiverRunFailedException)));
        }

        Assert.That(
            () => connectionUnrecoverableErrorCount > 0 ? serverResponseHasFinishedCount : serverResponseHasBegunCount,
            Is.EqualTo(connectionUnrecoverableErrorCount > 0 ? testCase.HandlerCount : testCase.ExpectedInitialConnectionCount)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        var observations = clientServiceProvider.GetRequiredService<TestObservations>();

        await testCase.PublishSignals(host.Resolve<ISignalPublishers>(), host.TestTimeoutToken);

        Assert.That(
            () => observations.ReceivedSignals,
            Is.EquivalentTo(testCase.ExpectedReceivedSignals)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        // return if no active receivers are expected
        if (testCase.ExpectedReceivedSignals.Count == 0)
        {
            return;
        }

        var handlerExceptions = testCase.HandlerExceptions
                                        .OfType<Exception>()
                                        .OrderBy(ex => ex.Message)
                                        .ToList();

        if (handlerExceptions.Count == 1)
        {
            await Assert.ThatAsync(
                async () => await await Task.WhenAny(run.CompletionTask, Task.Delay(host.AssertionTimeoutInMs, cts.Token)),
                Throws.InstanceOf<HttpSseSignalReceiverRunFailedException>()
                      .With.InnerException.SameAs(handlerExceptions[0]));
        }

        if (handlerExceptions.Count > 1)
        {
            await Assert.ThatAsync(
                async () => await await Task.WhenAny(run.CompletionTask, Task.Delay(host.AssertionTimeoutInMs, cts.Token)),
                Throws.InstanceOf<AggregateException>()
                      .With.Property("InnerExceptions")
                      .Count.EqualTo(handlerExceptions.Count)
                      .With.Property("InnerExceptions")
                      .Matches<ReadOnlyCollection<Exception>>(exs => exs.OfType<HttpSseSignalReceiverRunFailedException>()
                                                                        .Select(ex => ex.InnerException)
                                                                        .OfType<Exception>()
                                                                        .OrderBy(ex => ex.Message)
                                                                        .SequenceEqual(handlerExceptions)));
        }

        if (handlerExceptions.Count > 0)
        {
            Assert.That(
                () => serverResponseHasFinishedCount,
                Is.EqualTo(testCase.HandlerCount)
                  .After(host.AssertionTimeoutInMs)
                  .MilliSeconds
                  .PollEvery(10)
                  .MilliSeconds);

            return;
        }

        serverResponseHasBegunCount = 0;

        // this should trigger reconnects
        serverCancellationToken = null;
        await serverCts.CancelAsync();

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(testCase.HandlerCount)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        observations.ReceivedSignals.Clear();
        await testCase.PublishSignals(host.Resolve<ISignalPublishers>(), host.TestTimeoutToken);

        Assert.That(
            () => observations.ReceivedSignals,
            Is.EquivalentTo(testCase.ExpectedReceivedSignals)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);
    }

    [Test]
    public async Task GivenHttpSseSignalHandlerWithReconnectDelayFn_WhenRunningReceiverWithRecoverableErrors_ReconnectsAreExecutedAfterDelay()
    {
        await using var host = await CreateTestHost(
            services => services.AddConqueror()
                                .AddSingleton<TestObservations>()
                                .AddRouting(),
            app => app.MapSignalEndpoints());

        var httpClient = host.HttpClient;
        var observations = new TestObservations();

        using var serverCts = new CancellationTokenSource();
        serverCancellationToken = serverCts.Token;

        var taskCompletionSource1 = new TaskCompletionSource();
        var taskCompletionSource2 = new TaskCompletionSource();
        var taskCompletionSources = new Queue<TaskCompletionSource>([taskCompletionSource1, taskCompletionSource2]);
        var expectedStatusCodes = new Queue<int>([StatusCodes.Status503ServiceUnavailable, StatusCodes.Status200OK]);

        serverConnectionResponses.Enqueue((StatusCodes.Status503ServiceUnavailable, ContentTypes.TextPlain, KeepAlive: false));

        var clientServices = new ServiceCollection()
                             .AddSignalHandler<TestSignalHandler>()
                             .AddSingleton<FnToCallFromHandler>((s, _) =>
                             {
                                 observations.ReceivedSignals.Enqueue(s);

                                 return Task.CompletedTask;
                             })
                             .AddSingleton(observations)
                             .AddSingleton<Action<IHttpSseSignalReceiver>>(r => r.Enable(SseAddress)
                                                                                 .WithHttpClient(httpClient)

                                                                                 // test that function can be overwritten
                                                                                 .WithReconnectDelayFunction((_, _) => throw new NotSupportedException())
                                                                                 .WithReconnectDelayFunction(async (statusCode, ct) =>
                                                                                 {
                                                                                     ct.ThrowIfCancellationRequested();

                                                                                     Assert.That(
                                                                                         statusCode,
                                                                                         Is.EqualTo(expectedStatusCodes.Dequeue()));

                                                                                     var taskCompletionSource = taskCompletionSources.Dequeue();

                                                                                     await using var d = ct.Register(() => taskCompletionSource.TrySetCanceled());

                                                                                     await taskCompletionSource.Task;
                                                                                 }));

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        await using var run = signalReceivers.RunHttpSseSignalReceivers(host.TestTimeoutToken);

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(1)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await host.Resolve<ISignalPublishers>()
                  .For(TestSignal.T)
                  .WithTransport(b => b.UseHttpServerSentEvents())
                  .Handle(new() { Payload = 10 }, host.TestTimeoutToken);

        await Task.Delay(10, host.TestTimeoutToken);

        Assert.That(observations.ReceivedSignals, Is.Empty);

        taskCompletionSource1.SetResult();

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(2)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await host.Resolve<ISignalPublishers>()
                  .For(TestSignal.T)
                  .WithTransport(b => b.UseHttpServerSentEvents())
                  .Handle(new() { Payload = 20 }, host.TestTimeoutToken);

        Assert.That(
            () => observations.ReceivedSignals,
            Is.EqualTo(new[] { new TestSignal { Payload = 20 } })
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        // this should trigger reconnects
        serverCancellationToken = null;
        await serverCts.CancelAsync();

        await Task.Delay(10, host.TestTimeoutToken);

        // client should not have reconnected yet, so same count as before
        Assert.That(
            () => serverCallCount,
            Is.EqualTo(2)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        taskCompletionSource2.SetResult();

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(3)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await host.Resolve<ISignalPublishers>()
                  .For(TestSignal.T)
                  .WithTransport(b => b.UseHttpServerSentEvents())
                  .Handle(new() { Payload = 30 }, host.TestTimeoutToken);

        Assert.That(
            () => observations.ReceivedSignals,
            Is.EqualTo(new[] { new TestSignal { Payload = 20 }, new TestSignal { Payload = 30 } })
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);
    }

    [Test]
    public async Task GivenHttpSseSignalHandlerForMultipleSignalTypes_WhenRunningReceiver_OnlyConfiguresReceiverOnce()
    {
        await using var host = await CreateTestHost(
            services => services.AddConqueror()
                                .AddSingleton<TestObservations>()
                                .AddRouting(),
            app => app.MapSignalEndpoints());

        var httpClient = host.HttpClient;
        var configCount = 0;

        var clientServices = new ServiceCollection().AddSignalHandler<MultiTestSignalHandler>()
                                                    .AddSingleton<Action<IHttpSseSignalReceiver>>(r =>
                                                    {
                                                        configCount += 1;
                                                        _ = r.Enable(SseAddress)
                                                             .WithHttpClient(httpClient);
                                                    });

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var run = signalReceivers.RunHttpSseSignalReceivers(cts.Token);

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(1)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        Assert.That(configCount, Is.EqualTo(1));
    }

    private Task<HttpTransportTestHost> CreateTestHost(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configure)
    {
        return HttpTransportTestHost.Create(
            configureServices,
            app =>
            {
                _ = app.Use(async (ctx, next) =>
                       {
                           _ = Interlocked.Increment(ref serverCallCount);
                           receivedHeadersOnServer = ctx.Request.Headers;

                           ctx.Response.OnStarting(() =>
                           {
                               _ = Interlocked.Increment(ref serverResponseHasBegunCount);

                               return Task.CompletedTask;
                           });

                           try
                           {
                               await next();
                           }
                           finally
                           {
                               _ = Interlocked.Increment(ref serverResponseHasFinishedCount);
                           }
                       })
                       .Use(async (ctx, next) =>
                       {
                           try
                           {
                               await next();
                           }
                           catch (Exception ex)
                           {
                               ctx.RequestServices.GetRequiredService<ILogger<HttpTransportTestHost>>()
                                  .LogError(ex, "exception in request pipeline");

                               if (ctx.Response.HasStarted)
                               {
                                   return;
                               }

                               ctx.Response.StatusCode = 500;
                               await ctx.Response.WriteAsync($"internal server error\n{ex}");
                           }
                       })
                       .Use(async (ctx, next) =>
                       {
                           if (serverCancellationToken is null)
                           {
                               await next();

                               return;
                           }

                           using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                               ctx.RequestAborted,
                               serverCancellationToken.Value);

                           ctx.RequestAborted = cts.Token;

                           await next();
                       })
                       .Use(async (ctx, next) =>
                       {
                           if (serverConnectionResponses.TryDequeue(out var res) && res.HasValue)
                           {
                               ctx.Response.ContentType = res.Value.ContentType;
                               ctx.Response.StatusCode = res.Value.StatusCode;
                               await ctx.Response.Body.FlushAsync(ctx.RequestAborted);

                               if (res.Value.KeepAlive)
                               {
                                   try
                                   {
                                       await Task.Delay(TimeSpan.FromMinutes(1), ctx.RequestAborted);
                                   }
                                   catch (OperationCanceledException)
                                   {
                                       // nothing to do
                                   }
                               }

                               return;
                           }

                           await next();
                       });

                configure(app);
            });
    }

    private static IEnumerable<TestCaseData> GenerateErrorTestCaseData()
        => GenerateErrorTestCases().Select(testCase => new TestCaseData(testCase) { TestName = testCase.ToString() });

    private static IEnumerable<HttpSseSignalErrorTestCase> GenerateErrorTestCases()
    {
        yield return new()
        {
            TestName = "single handler with configuration error",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [new InvalidOperationException("configuration error")],
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 0,
            HandlerCount = 1,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceiver<ThrowingTestSignalHandler>(ct),
        };

        yield return new()
        {
            TestName = "multiple handlers, second with configuration error",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [null, new InvalidOperationException("configuration error")],
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 0,
            HandlerCount = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                     .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
        };

        yield return new()
        {
            TestName = "single handler with disconnect",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 1,
            HandlerCount = 1,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 10 }, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceiver<ThrowingTestSignalHandler>(ct),
        };

        yield return new()
        {
            TestName = "multiple handlers with disconnects",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 2,
            HandlerCount = 2,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 10 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal2.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload2 = 20 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                     .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
        };

        yield return new()
        {
            TestName = "single handler with unrecoverable connection error",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            ConnectionResponses = [(StatusCodes.Status400BadRequest, ContentTypes.TextPlain, KeepAlive: false)],
            ExpectedInitialConnectionCount = 1,
            HandlerCount = 1,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceiver<ThrowingTestSignalHandler>(ct),
        };

        yield return new()
        {
            TestName = "multiple handlers with unrecoverable connection errors",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            ConnectionResponses =
            [
                (StatusCodes.Status403Forbidden, ContentTypes.TextPlain, KeepAlive: false),
                (StatusCodes.Status409Conflict, ContentTypes.TextPlain, KeepAlive: false),
            ],
            ExpectedInitialConnectionCount = 2,
            HandlerCount = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                     .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
        };

        yield return new()
        {
            TestName = "multiple handlers, one of which with unrecoverable connection error",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            ConnectionResponses =
            [
                null,
                (StatusCodes.Status418ImATeapot, ContentTypes.EventStream, KeepAlive: false),
            ],
            ExpectedInitialConnectionCount = 2,
            HandlerCount = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                     .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
        };

        yield return new()
        {
            TestName = "single handler with invalid server response",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            ConnectionResponses = [(StatusCodes.Status200OK, ContentTypes.TextPlain, KeepAlive: true)],
            ExpectedInitialConnectionCount = 1,
            HandlerCount = 1,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceiver<ThrowingTestSignalHandler>(ct),
        };

        yield return new()
        {
            TestName = "multiple handlers with invalid server responses",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            ConnectionResponses =
            [
                (StatusCodes.Status200OK, ContentTypes.TextPlain, KeepAlive: true),
                (StatusCodes.Status200OK, ContentTypes.TextPlain, KeepAlive: true),
            ],
            ExpectedInitialConnectionCount = 2,
            HandlerCount = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                     .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
        };

        yield return new()
        {
            TestName = "multiple handlers, one of which with invalid server response",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            ConnectionResponses =
            [
                null,
                (StatusCodes.Status200OK, ContentTypes.TextPlain, KeepAlive: true),
            ],
            ExpectedInitialConnectionCount = 2,
            HandlerCount = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                     .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
        };

        yield return new()
        {
            TestName = "single handler with recoverable connection error",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            ConnectionResponses = [(StatusCodes.Status500InternalServerError, ContentTypes.TextPlain, KeepAlive: true)],
            ExpectedInitialConnectionCount = 2,
            HandlerCount = 1,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 10 }, ct);

                await p.For(TestSignal2.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload2 = 20 }, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceiver<ThrowingTestSignalHandler>(ct),
        };

        yield return new()
        {
            TestName = "single handler with multiple recoverable connection errors",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            ConnectionResponses =
            [
                (StatusCodes.Status503ServiceUnavailable, ContentTypes.TextPlain, KeepAlive: true),
                (StatusCodes.Status503ServiceUnavailable, ContentTypes.TextPlain, KeepAlive: true),
            ],
            ExpectedInitialConnectionCount = 3,
            HandlerCount = 1,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 10 }, ct);

                await p.For(TestSignal2.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload2 = 20 }, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceiver<ThrowingTestSignalHandler>(ct),
        };

        yield return new()
        {
            TestName = "multiple handlers with recoverable connection errors",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            ConnectionResponses =
            [
                (StatusCodes.Status502BadGateway, ContentTypes.TextPlain, KeepAlive: true),
                (StatusCodes.Status504GatewayTimeout, ContentTypes.TextPlain, KeepAlive: true),
            ],
            ExpectedInitialConnectionCount = 4,
            HandlerCount = 2,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 10 }, ct);

                await p.For(TestSignal2.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload2 = 20 }, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                     .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
        };

        yield return new()
        {
            TestName = "multiple handlers, one of which with recoverable connection error",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            ConnectionResponses =
            [
                null,
                (StatusCodes.Status501NotImplemented, ContentTypes.TextPlain, KeepAlive: true),
            ],
            ExpectedInitialConnectionCount = 3,
            HandlerCount = 2,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 10 }, ct);

                await p.For(TestSignal2.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload2 = 20 }, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                     .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
        };

        yield return new()
        {
            TestName = "single handler with handler exception",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 1,
            HandlerCount = 1,
            HandlerExceptions =
            [
                null,
                new InvalidOperationException("handler exception"),
            ],
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 10 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal2.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload2 = 20 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 30 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 40 }, ct);
            },
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceiver<ThrowingTestSignalHandler>(ct),
        };

        yield return new()
        {
            TestName = "multiple handlers with handler exceptions",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 2,
            HandlerCount = 2,
            HandlerExceptions =
            [
                null,
                null,
                null,
                new InvalidOperationException("handler exception 1"),
                new InvalidOperationException("handler exception 2"),
            ],
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 10 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal2.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload2 = 20 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 30 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 40 }, ct);
            },
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                     .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
        };

        yield return new()
        {
            TestName = "multiple handlers, one of which with handler exception",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 2,
            HandlerCount = 2,
            HandlerExceptions =
            [
                null,
                null,
                null,
                new InvalidOperationException("handler exception"),
            ],
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 10 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal2.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload2 = 20 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 30 }, ct);

                await Task.Delay(10, ct);

                await p.For(TestSignal.T)
                       .WithTransport(b => b.UseHttpServerSentEvents())
                       .Handle(new() { Payload = 40 }, ct);
            },
            RegisterHandlers = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                     .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunHttpSseSignalReceivers(ct),
        };
    }

    private sealed record HttpSseSignalErrorTestCase
    {
        public required string TestName { get; init; }

        public required List<object> ExpectedReceivedSignals { get; init; }

        public required List<Exception?> ConfigurationExceptions { get; init; }

        public required List<Exception?> HandlerExceptions { get; init; }

        public required List<(int StatusCode, string ContentType, bool KeepAlive)?> ConnectionResponses { get; init; }

        public required int HandlerCount { get; init; }

        public required int ExpectedInitialConnectionCount { get; init; }

        public required Action<IServiceCollection> RegisterHandlers { get; init; }

        public required Func<ISignalPublishers, CancellationToken, Task> PublishSignals { get; init; }

        public required Func<ISignalReceivers, CancellationToken, SignalReceiverRun> RunReceivers { get; init; }

        public IServiceCollection RegisterClientServices(IServiceCollection services)
        {
            RegisterHandlers(services);

            return services.AddSingleton<TestObservations>()
                           .AddTransient(typeof(TestSignalMiddleware<>));
        }

        public void RegisterServerServices(IServiceCollection services)
        {
            _ = services.AddConqueror()
                        .AddSingleton<TestObservations>()
                        .AddTransient(typeof(TestSignalMiddleware<>))
                        .AddRouting();
        }

        public override string ToString() => TestName;
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

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver) => receiver.Enable(SseAddress);
    }

    private sealed partial class ThrowingTestSignalHandler(TestObservations observations, ConcurrentQueue<Exception?> exceptions)
        : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ReceivedSignals.Enqueue(signal);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                throw ex;
            }
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);
    }

    private sealed partial class ThrowingTestSignalHandler2(TestObservations observations, ConcurrentQueue<Exception?> exceptions)
        : TestSignal.IHandler,
          TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ReceivedSignals.Enqueue(signal);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                throw ex;
            }
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ReceivedSignals.Enqueue(signal);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                throw ex;
            }
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);
    }
}
