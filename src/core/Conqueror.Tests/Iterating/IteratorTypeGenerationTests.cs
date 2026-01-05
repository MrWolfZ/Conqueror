// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

// we simulate the generator output here
#pragma warning disable SA1302, CA1715

namespace Conqueror.Tests.Iterating;

using System.Runtime.CompilerServices;

[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = "we are emulating the output of the source generator"
)]
[SuppressMessage(
    "Roslynator",
    "RCS1018:Add/remove accessibility modifiers",
    Justification = "we are emulating the output of the source generator"
)]
public sealed partial class IteratorTypeGenerationTests
{
    [Test]
    public async Task GivenIteratorTypeWithExplicitImplementations_WhenUsingHandler_ItWorks()
    {
        var services = new ServiceCollection();
        var provider = services.AddIteratorHandler<TestIteratorHandler>().BuildServiceProvider();

        var iterators = provider.GetRequiredService<IIterators>();

        var items = new List<int>();
        await foreach (
            var item in iterators
                .For(TestIterator.T)
                .WithPipeline(p => p.UseTest().UseTest())
                .WithTransport(b => b.UseInProcess())
                .Handle(new(Count: 3), CancellationToken.None)
        )
        {
            items.Add(item);
        }

        Assert.That(items, Is.EqualTo([1, 2, 3]));
    }

    [Iterator<int>]
    public sealed partial record TestIterator(int Count);

    // generated
    public sealed partial record TestIterator : IIterator<TestIterator, int>
    {
        public static IteratorTypes<TestIterator, int, IHandler> T => new();

        static IIteratorHandlerTypesInjector IIterator<TestIterator, int>.CoreTypesInjector { get; } =
            IHandler.CreateCoreTypesInjector();

        [EditorBrowsable(EditorBrowsableState.Never)]
        static TestIterator? IIterator<TestIterator, int>.EmptyInstance => null;

        static IEnumerable<ConstructorInfo> IIterator<TestIterator, int>.PublicConstructors =>
            typeof(TestIterator).GetConstructors(BindingFlags.Public);

        static IEnumerable<PropertyInfo> IIterator<TestIterator, int>.PublicProperties =>
            typeof(TestIterator).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        static IAsyncEnumerable<int> IIterator<TestIterator, int>.InvokeHandler<TIHandler>(
            TIHandler handler,
            TestIterator iterator,
            CancellationToken cancellationToken
        ) => ((IHandler)handler).Handle(iterator, cancellationToken);

        public interface IHandler
            : IIteratorHandler<TestIterator, int, IHandler, IHandler.Proxy, IPipeline, IPipeline.Proxy>
        {
            IAsyncEnumerable<int> Handle(TestIterator iterator, CancellationToken cancellationToken = default);

            [EditorBrowsable(EditorBrowsableState.Never)]
            sealed class Proxy : IteratorHandlerProxy<TestIterator, int, IHandler>, IHandler;
        }

        public interface IPipeline : IIteratorPipeline<TestIterator, int>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            sealed class Proxy : IteratorPipelineProxy<TestIterator, int>, IPipeline;
        }
    }

    private sealed partial class TestIteratorHandler : TestIterator.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.CompletedTask;

            for (var i = 1; i <= iterator.Count; i++)
            {
                yield return i;
            }
        }

        public static void ConfigurePipeline(TestIterator.IPipeline pipeline) => pipeline.UseTest().UseTest();

        static IEnumerable<IIteratorHandlerTypesInjector> IIteratorHandler.GetTypeInjectors()
        {
            yield return TestIterator.IHandler.CreateCoreTypesInjector<TestIteratorHandler>();
        }
    }
}

public static class IteratorTypeGenerationTestsPipelineExtensions
{
    public static IIteratorPipeline<TIterator, TItem> UseTest<TIterator, TItem>(
        this IIteratorPipeline<TIterator, TItem> pipeline
    )
        where TIterator : class, IIterator<TIterator, TItem> =>
        pipeline.Use(ctx => ctx.Next(ctx.Iterator, ctx.CancellationToken));
}
