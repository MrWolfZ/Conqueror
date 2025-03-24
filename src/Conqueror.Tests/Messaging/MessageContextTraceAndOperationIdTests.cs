using System.Diagnostics;

namespace Conqueror.Tests.Messaging;

public sealed partial class MessageContextTraceAndOperationIdTests
{
    private static int testCaseCounter;

    [Test]
    [Combinatorial]
    [SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "parameter name makes sense here")]
    public async Task GivenSetup_WhenExecutingHandler_OperationIdsAreCorrectlyAvailable(
        [Values(true, false)] bool hasCustomTraceId,
        [Values(true, false)] bool hasActivity)
    {
        var customTraceId = Guid.NewGuid().ToString();

        string? traceIdFromClientTransportBuilder = null;
        string? messageIdFromClientTransportBuilder = null;
        string? traceIdFromMessageHandler = null;
        string? messageIdFromMessageHandler = null;
        string? traceIdFromNestedMessageHandler = null;
        string? messageIdFromNestedMessageHandler = null;

        var services = new ServiceCollection();

        _ = services.AddConquerorMessageHandlerDelegate<TestMessage, TestMessageResponse>(async (cmd, p, ct) =>
                    {
                        await Task.Yield();
                        traceIdFromMessageHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetTraceId();
                        messageIdFromMessageHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetMessageId();
                        _ = await p.GetRequiredService<IMessageClients>().For<NestedTestMessage.IHandler>().Handle(new(), ct);
                        return new();
                    })
                    .AddConquerorMessageHandlerDelegate<NestedTestMessage, TestMessageResponse>(async (_, p, _) =>
                    {
                        await Task.Yield();
                        traceIdFromNestedMessageHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetTraceId();
                        messageIdFromNestedMessageHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetMessageId();
                        return new();
                    });

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        ConquerorContext? conquerorContext = null;

        if (hasCustomTraceId)
        {
            conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();
            conquerorContext.SetTraceId(customTraceId);
        }

        using var d = conquerorContext;

        var testCaseIdx = Interlocked.Increment(ref testCaseCounter);
        using var activity = hasActivity ? StartActivity(nameof(MessageContextTraceAndOperationIdTests) + testCaseIdx) : null;

        var handlerClient = serviceProvider.GetRequiredService<IMessageClients>()
                                           .For<TestMessage.IHandler>()
                                           .WithTransport(b =>
                                           {
                                               traceIdFromClientTransportBuilder = b.ConquerorContext.GetTraceId();
                                               messageIdFromClientTransportBuilder = b.ConquerorContext.GetMessageId();
                                               return b.UseInProcess();
                                           });

        _ = await handlerClient.Handle(new());

        var expectedTraceId = (hasCustomTraceId, hasActivity) switch
        {
            (true, _) => customTraceId,
            (false, true) => activity!.TraceId,
            (false, false) => traceIdFromClientTransportBuilder,
        };

        Assert.Multiple(() =>
        {
            Assert.That(traceIdFromClientTransportBuilder, Is.EqualTo(expectedTraceId));
            Assert.That(traceIdFromMessageHandler, Is.EqualTo(expectedTraceId));
            Assert.That(traceIdFromNestedMessageHandler, Is.EqualTo(expectedTraceId));

            Assert.That(messageIdFromClientTransportBuilder, Is.EqualTo(messageIdFromMessageHandler));
            Assert.That(messageIdFromMessageHandler, Is.Not.Null);
            Assert.That(messageIdFromNestedMessageHandler, Is.Not.EqualTo(messageIdFromMessageHandler));
        });
    }

    private static DisposableActivity StartActivity(string name)
    {
        var activitySource = new ActivitySource(name);

        var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(activityListener);

        var activity = activitySource.StartActivity()!;
        return new(activity.TraceId.ToString(), activitySource, activityListener, activity);
    }

    private sealed class DisposableActivity(string traceId, params IDisposable[] disposables) : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables = disposables;

        public string TraceId { get; } = traceId;

        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }

    [Message<TestMessageResponse>]
    private sealed partial record TestMessage;

    private sealed record TestMessageResponse;

    [Message<TestMessageResponse>]
    private sealed partial record NestedTestMessage;
}
