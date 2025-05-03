using Polly;

namespace Conqueror.Middleware.Polly.Tests.Messaging;

public sealed partial class PollyMessageMiddlewareTests
{
    [Test]
    [Combinatorial]
    public async Task GivenHandlerWithPollyMiddlewareWithPolicy_WhenCallingHandler_ExecutesPipelineWithPolicy(
        [Values] bool shouldConfigureInitialBuilder,
        [Values] bool shouldConfigureAdditionalBuilder,
        [Values] bool shouldRemoveMiddleware)
    {
        var executionCount = 0;

        var message = new TestMessage { Payload = 10 };

        await using var host = await PollyMiddlewareTestHost.Create(services =>
        {
            _ = services.AddMessageHandlerDelegate(
                TestMessage.T,
                (msg, _, _) =>
                {
                    Assert.That(msg, Is.SameAs(message));

                    executionCount += 1;

                    if (executionCount < 2)
                    {
                        throw new IOException();
                    }

                    return new(msg.Payload + 1);
                });
        });

        var handler = host.Resolve<IMessageSenders>()
                          .For(TestMessage.T)
                          .WithPipeline(p =>
                          {
                              var initialP = p;

                              p = shouldConfigureInitialBuilder
                                  ? p.UsePolly(b => b.AddRetry(new() { Delay = TimeSpan.Zero }))
                                  : p.UsePolly();

                              if (shouldConfigureAdditionalBuilder)
                              {
                                  p = p.ConfigurePolly(b => b.AddRetry(new() { Delay = TimeSpan.Zero }));
                              }

                              if (shouldRemoveMiddleware)
                              {
                                  p = p.WithoutPolly();
                              }

                              Assert.That(p, Is.SameAs(initialP));
                          });

        if ((shouldConfigureInitialBuilder || shouldConfigureAdditionalBuilder) && !shouldRemoveMiddleware)
        {
            var response = await handler.Handle(message, host.TestTimeoutToken);

            Assert.That(response.Payload, Is.EqualTo(11));
            Assert.That(executionCount, Is.EqualTo(2));

            return;
        }

        await Assert.ThatAsync(
            () => handler.Handle(message, host.TestTimeoutToken),
            Throws.TypeOf<IOException>());

        Assert.That(executionCount, Is.EqualTo(1));
    }

    [Message<TestMessageResponse>]
    private sealed partial class TestMessage
    {
        public required int Payload { get; init; }
    }

    private sealed record TestMessageResponse(int Payload);
}
