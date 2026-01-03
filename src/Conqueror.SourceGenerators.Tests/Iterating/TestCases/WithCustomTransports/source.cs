#nullable enable

namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.WithCustomTransports
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using global::Iterating.WithCustomTransports.Transport1;
    using global::Iterating.WithCustomTransports.Transport2;

    [Iterator<TestItem>]
    [TestTransportIterator<TestItem>(StringProperty = "Test")]
    [TestTransport2Iterator<TestItem>(StringProperty = "Test2")]
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

namespace Iterating.WithCustomTransports.Transport1
{
    using System;
    using Conqueror;
    using Conqueror.Iterating;

    [IteratorTransport(Prefix = "TestTransport", Namespace = "Iterating.WithCustomTransports.Transport1")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransportIteratorAttribute : Attribute
    {
        public string? StringProperty { get; init; }
    }

    [IteratorTransport(Prefix = "TestTransport", Namespace = "Iterating.WithCustomTransports.Transport1")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportIteratorAttribute<TItem> : TestTransportIteratorAttribute;

    public interface ITestTransportIterator<TIterator, TItem> : IIterator<TIterator, TItem>
        where TIterator : class, ITestTransportIterator<TIterator, TItem>
    {
        static virtual string StringProperty => "Default";
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

namespace Iterating.WithCustomTransports.Transport2
{
    using System;
    using Conqueror;
    using Conqueror.Iterating;

    [IteratorTransport(Prefix = "TestTransport2", Namespace = "Iterating.WithCustomTransports.Transport2")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransport2IteratorAttribute : Attribute
    {
        public string? StringProperty { get; init; }
    }

    [IteratorTransport(Prefix = "TestTransport2", Namespace = "Iterating.WithCustomTransports.Transport2")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransport2IteratorAttribute<TItem> : TestTransport2IteratorAttribute;

    public interface ITestTransport2Iterator<TIterator, TItem> : IIterator<TIterator, TItem>
        where TIterator : class, ITestTransport2Iterator<TIterator, TItem>
    {
        static virtual string? StringProperty { get; }
    }

    public interface ITestTransport2IteratorHandler;

    public interface ITestTransport2IteratorHandler<TIterator, TItem, TIHandler>
        where TIterator : class, ITestTransport2Iterator<TIterator, TItem>
        where TIHandler : class, ITestTransport2IteratorHandler<TIterator, TItem, TIHandler>
    {
        static IIteratorHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
            where THandler : class, TIHandler => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.WithCustomTransports
{
    public partial record TestIterator
    {
        public partial interface IHandler;
    }
}
