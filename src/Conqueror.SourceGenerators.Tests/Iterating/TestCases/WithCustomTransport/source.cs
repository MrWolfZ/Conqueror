#nullable enable

namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.WithCustomTransport
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using global::Iterating.WithCustomTransport;

    [TestTransportIterator<TestItem>(
        StringProperty = "Test",
        IntProperty = 1,
        IntArrayProperty = [1, 2, 3],
        NullProperty = null
    )]
    public partial record TestIterator;

    public record TestItem;

    public partial class TestIteratorHandler : TestIterator.IHandler
    {
        public async IAsyncEnumerable<TestItem> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await System.Threading.Tasks.Task.CompletedTask;
            yield break;
        }
    }
}

namespace Iterating.WithCustomTransport
{
    using System;
    using Conqueror;
    using Conqueror.Iterating;

    [IteratorTransport(Prefix = "TestTransport", Namespace = "Iterating.WithCustomTransport")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransportIteratorAttribute : Attribute
    {
        public string? StringProperty { get; init; }

        public int IntProperty { get; init; }

        public int[]? IntArrayProperty { get; init; }

        public string? NullProperty { get; init; }

        public string? UnsetProperty { get; init; }
    }

    [IteratorTransport(Prefix = "TestTransport", Namespace = "Iterating.WithCustomTransport")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportIteratorAttribute<TItem> : TestTransportIteratorAttribute;

    public interface ITestTransportIterator<TIterator, TItem> : IIterator<TIterator, TItem>
        where TIterator : class, ITestTransportIterator<TIterator, TItem>
    {
        static virtual string StringProperty => "Default";

        static virtual int IntProperty { get; }

        static virtual int[] IntArrayProperty { get; } = [];

        static virtual string? NullProperty { get; }

        static virtual string? UnsetProperty { get; }
    }

    public interface ITestTransportIteratorHandler;

    public interface ITestTransportIteratorHandler<TIterator, TItem, TIHandler>
        where TIterator : class, ITestTransportIterator<TIterator, TItem>
        where TIHandler : class, ITestTransportIteratorHandler<TIterator, TItem, TIHandler>
    {
        static IIteratorHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
            where THandler : class, TIHandler => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.WithCustomTransport
{
    public partial record TestIterator
    {
        public partial interface IHandler;
    }
}
