using System.Diagnostics;

namespace Conqueror.Tests.Messaging;

public sealed partial class MessageContextTraceAndOperationIdTests
{
    private static int testCaseCounter;

    [Test]
    [Combinatorial]
    public async Task GivenSetup_WhenExecutingHandler_OperationIdsAreCorrectlyAvailable(
        [Values(true, false)] bool hasCustomTraceId,
        [Values(true, false)] bool hasActivity,
        [Values(true, false)] bool sendNestedWithDifferentTransport)
    {
        var customTraceId = Guid.NewGuid().ToString();

        string? traceIdFromTransportBuilder = null;
        string? messageIdFromTransportBuilder = null;
        string? traceIdFromHandler = null;
        string? messageIdFromHandler = null;
        string? traceIdFromNestedMessageHandler = null;
        string? messageIdFromNestedMessageHandler = null;

        var services = new ServiceCollection();

        _ = services.AddMessageHandlerDelegate(
                        TestMessage.T,
                        async (msg, p, ct) =>
                        {
                            await Task.Yield();
                            traceIdFromHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetTraceId();
                            messageIdFromHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetMessageId();

                            var handler = p.GetRequiredService<IMessageSenders>()
                                           .For(NestedTestMessage.T);

                            if (sendNestedWithDifferentTransport)
                            {
                                handler = handler.WithTransport(b => new TestMessageSender<NestedTestMessage, TestMessageResponse>(
                                                                    b.UseInProcess()));
                            }

                            _ = await handler.Handle(new(), ct);

                            return new();
                        })
                    .AddMessageHandlerDelegate(
                        NestedTestMessage.T,
                        async (_, p, _) =>
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

        var handlerSender = serviceProvider.GetRequiredService<IMessageSenders>()
                                           .For(TestMessage.T)
                                           .WithTransport(b =>
                                           {
                                               traceIdFromTransportBuilder = b.ConquerorContext.GetTraceId();
                                               messageIdFromTransportBuilder = b.ConquerorContext.GetMessageId();

                                               return b.UseInProcess();
                                           });

        _ = await handlerSender.Handle(new());

        var expectedTraceId = (hasCustomTraceId, hasActivity) switch
        {
            (true, _) => customTraceId,
            (false, true) => activity!.TraceId,
            (false, false) => traceIdFromTransportBuilder,
        };

        Assert.Multiple(() =>
        {
            Assert.That(traceIdFromTransportBuilder, Is.EqualTo(expectedTraceId));
            Assert.That(traceIdFromHandler, Is.EqualTo(expectedTraceId));
            Assert.That(traceIdFromNestedMessageHandler, Is.EqualTo(expectedTraceId));

            Assert.That(messageIdFromTransportBuilder, Is.EqualTo(messageIdFromHandler));
            Assert.That(messageIdFromHandler, Is.Not.Null);
            Assert.That(messageIdFromNestedMessageHandler, Is.Not.EqualTo(messageIdFromHandler));
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

        return new(
            activity.TraceId.ToString(),
            activitySource,
            activityListener,
            activity);
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

    private sealed class TestMessageSender<TMessage, TResponse>(IMessageSender<TMessage, TResponse> wrapped) : IMessageSender<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public string TransportTypeName => "test";

        public Task<TResponse> Send(
            TMessage message,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            CancellationToken cancellationToken)
        {
            return wrapped.Send(
                message,
                serviceProvider,
                conquerorContext,
                cancellationToken);
        }
    }
}
