using Polly;

namespace Conqueror.Middleware.Polly.Tests.Signalling;

public sealed partial class PollySignalMiddlewareTests
{
    [Test]
    [Combinatorial]
    public async Task GivenHandlerWithPollyMiddlewareWithPolicy_WhenCallingHandler_ExecutesPipelineWithPolicy(
        [Values] bool shouldConfigureInitialBuilder,
        [Values] bool shouldConfigureAdditionalBuilder,
        [Values] bool shouldRemoveMiddleware)
    {
        var executionCount = 0;

        var signal = new TestSignal { Payload = 10 };

        await using var host = await PollyMiddlewareTestHost.Create(services =>
        {
            _ = services.AddSignalHandlerDelegate(
                TestSignal.T,
                (s, _, _) =>
                {
                    Assert.That(s, Is.SameAs(signal));

                    executionCount += 1;

                    if (executionCount < 2)
                    {
                        throw new IOException();
                    }
                });
        });

        var handler = host.Resolve<ISignalPublishers>()
                          .For(TestSignal.T)
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
            await handler.Handle(signal, host.TestTimeoutToken);

            Assert.That(executionCount, Is.EqualTo(2));

            return;
        }

        await Assert.ThatAsync(
            () => handler.Handle(signal, host.TestTimeoutToken),
            Throws.TypeOf<IOException>());

        Assert.That(executionCount, Is.EqualTo(1));
    }

    [Signal]
    private sealed partial class TestSignal
    {
        public required int Payload { get; init; }
    }
}
