namespace Conqueror.Tests.Iterating;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[SuppressMessage(
    "Style",
    "MA0032:Use an overload with a CancellationToken",
    Justification = "Tests are intentionally validating token flow"
)]
public sealed partial class IteratorMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithParameter_WhenPipelineConfigurationUpdatesParameter_TheMiddlewareExecutesWithUpdatedParameter()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddIteratorHandler<TestIteratorHandler>().AddSingleton(observations);

        _ = services.AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
        {
            var testObservations = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            _ = pipeline.Use(new TestIteratorMiddleware<TestIterator, int>(testObservations) { Parameter = 10 });

            _ = pipeline.Configure<TestIteratorMiddleware<TestIterator, int>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        _ = await ConsumeAll(handler.Handle(new(), CancellationToken.None));

        Assert.That(observations.Parameters, Is.EqualTo([20]));
    }

    [Test]
    public async Task GivenMultipleMiddlewareOfSameType_WhenPipelineConfigurationRuns_AllMiddlewaresAreUpdated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddIteratorHandler<TestIteratorHandler>().AddSingleton(observations);

        _ = services.AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
        {
            var testObservations = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            _ = pipeline
                .Use(new TestIteratorMiddleware<TestIterator, int>(testObservations) { Parameter = 10 })
                .Use(new TestIteratorMiddleware<TestIterator, int>(testObservations) { Parameter = 30 })
                .Use(new TestIteratorMiddleware<TestIterator, int>(testObservations) { Parameter = 50 });

            _ = pipeline.Configure<TestIteratorMiddleware<TestIterator, int>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        _ = await ConsumeAll(handler.Handle(new(), CancellationToken.None));

        Assert.That(observations.Parameters, Is.EqualTo([20, 40, 60]));
    }

    [Test]
    public async Task GivenMiddlewareWithBaseClass_WhenPipelineConfiguresBaseClass_TheMiddlewareIsConfigured()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddIteratorHandler<TestIteratorHandler>().AddSingleton(observations);

        _ = services.AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
        {
            var testObservations = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            _ = pipeline.Use(new TestIteratorMiddlewareSub<TestIterator, int>(testObservations) { Parameter = 10 });

            _ = pipeline.Configure<TestIteratorMiddlewareBase<TestIterator, int>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        _ = await ConsumeAll(handler.Handle(new(), CancellationToken.None));

        Assert.That(observations.Parameters, Is.EqualTo([20]));
    }

    [Test]
    public async Task GivenUnusedMiddleware_WhenConfiguringMiddleware_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddIteratorHandler<TestIteratorHandler>().AddSingleton(observations);

        _ = services.AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
        {
            _ = Assert.Throws<InvalidOperationException>(() =>
                pipeline.Configure<TestIteratorMiddleware<TestIterator, int>>(c => c.Parameter += 10)
            );
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        _ = await ConsumeAll(handler.Handle(new(), CancellationToken.None));
    }

    [Test]
    public async Task GivenConditionalMiddlewareWithParameter_WhenPipelineConfigurationUpdatesParameter_TheMiddlewareExecutesWithUpdatedParameter()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddIteratorHandler<TestIteratorHandler>().AddSingleton(observations);

        _ = services.AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
        {
            _ = pipeline.UseWhen(
                _ => true,
                p =>
                {
                    var testObservations = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                    _ = p.Use(new TestIteratorMiddleware<TestIterator, int>(testObservations) { Parameter = 10 });
                }
            );

            _ = pipeline.Configure<TestIteratorMiddleware<TestIterator, int>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        _ = await ConsumeAll(handler.Handle(new(), CancellationToken.None));

        Assert.That(observations.Parameters, Is.EqualTo([20]));
    }

    [Iterator<int>]
    private sealed partial record TestIterator;

    private sealed partial class TestIteratorHandler : TestIterator.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            yield return 1;
        }

        public static void ConfigurePipeline(TestIterator.IPipeline pipeline) =>
            pipeline.ServiceProvider.GetService<Action<TestIterator.IPipeline>>()?.Invoke(pipeline);
    }

    private sealed class TestIteratorMiddleware<TIterator, TItem>(TestObservations observations)
        : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public int Parameter { get; set; }

        public async IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class TestIteratorMiddlewareSub<TIterator, TItem>(TestObservations observations)
        : TestIteratorMiddlewareBase<TIterator, TItem>(observations)
        where TIterator : class, IIterator<TIterator, TItem>;

    private abstract class TestIteratorMiddlewareBase<TIterator, TItem>(TestObservations observations)
        : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public int Parameter { get; set; }

        public async IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class TestObservations
    {
        public List<int> Parameters { get; } = [];
    }

    private static async Task<List<T>> ConsumeAll<T>(
        IAsyncEnumerable<T> enumerable,
        CancellationToken cancellationToken = default
    )
    {
        var list = new List<T>();
        await foreach (var item in enumerable.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }

        return list;
    }
}
