#nullable enable

namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.WithCustomTransportWithHierarchy
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using global::Iterating.WithCustomTransportWithHierarchy;

    [TestTransportIterator<TestItem>]
    public partial record TestIterator(int Payload);

    [TestTransportIterator<TestItem>]
    public partial record TestIteratorSub(int Payload) : TestIterator(Payload);

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

    public partial class TestIteratorSubHandler : TestIteratorSub.IHandler
    {
        public async IAsyncEnumerable<TestItem> Handle(
            TestIteratorSub iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await System.Threading.Tasks.Task.CompletedTask;
            yield break;
        }
    }
}

namespace Iterating.WithCustomTransportWithHierarchy
{
    using System;
    using Conqueror;
    using Conqueror.Iterating;

    [IteratorTransport(Prefix = "TestTransport", Namespace = "Iterating.WithCustomTransportWithHierarchy")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransportIteratorAttribute : Attribute;

    [IteratorTransport(Prefix = "TestTransport", Namespace = "Iterating.WithCustomTransportWithHierarchy")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportIteratorAttribute<TItem> : TestTransportIteratorAttribute;

    public interface ITestTransportIterator<TIterator, TItem> : IIterator<TIterator, TItem>
        where TIterator : class, ITestTransportIterator<TIterator, TItem>;

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
namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.WithCustomTransportWithHierarchy
{
    public partial record TestIterator
    {
        public partial interface IHandler;
    }

    public partial record TestIteratorSub
    {
        public new partial interface IHandler;
    }
}
