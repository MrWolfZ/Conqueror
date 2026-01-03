#nullable enable

namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.WithCustomTransportWithIteratorTypeOverride
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using WithCustomTransportWithIteratorTypeOverrideCustomTransport;

    [CustomTestTransportIterator<TestItem>(ExtraProperty = "Test")]
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

namespace WithCustomTransportWithIteratorTypeOverrideOriginalTransport
{
    using System;
    using Conqueror;
    using Conqueror.Iterating;

    [IteratorTransport(
        Prefix = "TestTransport",
        Namespace = "WithCustomTransportWithIteratorTypeOverrideOriginalTransport"
    )]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransportIteratorAttribute : Attribute
    {
        public string? StringProperty { get; init; }
    }

    [IteratorTransport(
        Prefix = "TestTransport",
        Namespace = "WithCustomTransportWithIteratorTypeOverrideOriginalTransport"
    )]
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

namespace WithCustomTransportWithIteratorTypeOverrideCustomTransport
{
    using System;
    using Conqueror.Iterating;
    using WithCustomTransportWithIteratorTypeOverrideOriginalTransport;

    [IteratorTransport(
        Prefix = "TestTransport",
        Namespace = "WithCustomTransportWithIteratorTypeOverrideOriginalTransport",
        FullyQualifiedIteratorTypeName = "WithCustomTransportWithIteratorTypeOverrideCustomTransport.ICustomTestTransportIterator"
    )]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CustomTestTransportIteratorAttribute : Attribute
    {
        public string? ExtraProperty { get; init; }
    }

    [IteratorTransport(
        Prefix = "TestTransport",
        Namespace = "WithCustomTransportWithIteratorTypeOverrideOriginalTransport",
        FullyQualifiedIteratorTypeName = "WithCustomTransportWithIteratorTypeOverrideCustomTransport.ICustomTestTransportIterator"
    )]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CustomTestTransportIteratorAttribute<TItem> : CustomTestTransportIteratorAttribute;

    public interface ICustomTestTransportIterator<TIterator, TItem> : ITestTransportIterator<TIterator, TItem>
        where TIterator : class, ICustomTestTransportIterator<TIterator, TItem>
    {
        static virtual string? ExtraProperty { get; }

        static string ITestTransportIterator<TIterator, TItem>.StringProperty { get; } =
            TIterator.ExtraProperty ?? "Default";
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.WithCustomTransportWithIteratorTypeOverride
{
    public partial record TestIterator
    {
        public partial interface IHandler;
    }
}
