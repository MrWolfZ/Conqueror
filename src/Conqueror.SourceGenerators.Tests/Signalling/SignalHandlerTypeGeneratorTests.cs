using System.Reflection;
using Conqueror.SourceGenerators.Signalling;
using Conqueror.SourceGenerators.TestUtil;
using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Tests.Signalling;

[TestFixture]
public sealed class SignalHandlerTypeGeneratorTests
{
    private readonly IReadOnlyCollection<IIncrementalGenerator> generators = [new SignalTypeGenerator(), new SignalHandlerTypeGenerator()];
    private readonly IReadOnlyCollection<Assembly> assembliesToLoad = [typeof(ISignalIdFactory).Assembly];

    [Test]
    public Task GivenTestSignalHandler_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[Signal]
public partial record TestSignal;

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal signal, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalHandlerForMultipleSignalTypes_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[Signal]
public partial record TestSignal;

[Signal]
public partial record TestSignal2;

public partial class TestSignalHandler : TestSignal.IHandler,
                                         TestSignal2.IHandler
{
    public Task Handle(TestSignal signal, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task Handle(TestSignal2 signal, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalHandlerWithBaseClassForMultipleSignalTypes_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[Signal]
public partial record TestSignal;

[Signal]
public partial record TestSignal2;

public partial record FakeSignal
{
    public interface IHandler;
}

public abstract class BaseHandler;

public partial class TestSignalHandler : BaseHandler,
                                         TestSignal.IHandler,
                                         TestSignal2.IHandler,
                                         FakeSignal.IHandler
{
    public Task Handle(TestSignal signal, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task Handle(TestSignal2 signal, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalHandlerWithImplementedGetTypeInjectorsMethod_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[Signal]
public partial record TestSignal;

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal signal, CancellationToken cancellationToken) => throw new NotImplementedException();

    static IEnumerable<ISignalHandlerTypesInjector> ISignalHandler.GetTypeInjectors() => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalHandlerForTransport_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Signalling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[SignalTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportSignalAttribute : Attribute
{
    public string? StringProperty { get; init; }

    public int IntProperty { get; init; }

    public int[]? IntArrayProperty { get; init; }

    public string? NullProperty { get; init; }

    public string? UnsetProperty { get; init; }
}

public interface ITestTransportSignal<TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransportSignal<TSignal>
{
    static virtual string StringProperty { get; set; } = ""Default"";

    static virtual int IntProperty { get; set; }

    static virtual int[] IntArrayProperty { get; set; } = [];

    static virtual string? NullProperty { get; set; }

    static virtual string? UnsetProperty { get; set; }
}

public interface ITestTransportSignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
{
    static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

[TestTransportSignal(StringProperty = ""Test"", IntProperty = 1, IntArrayProperty = new[] { 1, 2, 3 }, NullProperty = null)]
public partial record TestSignal;

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal signal, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalHandlerForMultipleTransports_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Signalling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[SignalTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportSignalAttribute : Attribute
{
    public string? StringProperty { get; init; }
}

[SignalTransport(Prefix = ""TestTransport2"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2SignalAttribute : Attribute;

public interface ITestTransportSignal<TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransportSignal<TSignal>
{
    static virtual string StringProperty { get; set; } = ""Default"";
}

public interface ITestTransport2Signal<TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransport2Signal<TSignal>
{
    static virtual string? StringProperty { get; set; }
}

public interface ITestTransportSignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
{
    static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

public interface ITestTransport2SignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransport2Signal<TSignal>
    where TIHandler : class, ITestTransport2SignalHandler<TSignal, TIHandler>
{
    static ISignalHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

[Signal]
[TestTransportSignal(StringProperty = ""Test"")]
[TestTransport2Signal]
public partial record TestSignal;

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal signal, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalHandlerForMultipleMixedTransports_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Signalling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[SignalTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportSignalAttribute : Attribute
{
    public string? StringProperty { get; init; }
}

[SignalTransport(Prefix = ""TestTransport2"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2SignalAttribute : Attribute;

public interface ITestTransportSignal<TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransportSignal<TSignal>
{
    static virtual string StringProperty { get; set; } = ""Default"";
}

public interface ITestTransport2Signal<TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransport2Signal<TSignal>
{
    static virtual string? StringProperty { get; set; }
}

public interface ITestTransportSignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
{
    static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

public interface ITestTransport2SignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransport2Signal<TSignal>
    where TIHandler : class, ITestTransport2SignalHandler<TSignal, TIHandler>
{
    static ISignalHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

[TestTransportSignal(StringProperty = ""Test"")]
[TestTransport2Signal]
public partial record TestSignal;

[TestTransport2Signal]
public partial record TestSignal2;

public partial class TestSignalHandler : TestSignal.IHandler,
                                         TestSignal2.IHandler
{
    public Task Handle(TestSignal signal, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task Handle(TestSignal2 signal, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    private static VerifySettings Settings()
    {
        var settings = new VerifySettings();
        settings.ScrubLinesWithReplace(line => line.ReplaceGeneratorVersion());
        settings.UseDirectory("Snapshots");
        settings.UseTypeName(nameof(SignalHandlerTypeGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
