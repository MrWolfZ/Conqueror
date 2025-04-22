using System.Reflection;
using System.Text.Json.Serialization;
using Conqueror.SourceGenerators.Signalling;
using Conqueror.SourceGenerators.TestUtil;
using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Tests.Signalling;

[TestFixture]
public sealed class SignalTypeGeneratorTests
{
    private readonly IReadOnlyCollection<IIncrementalGenerator> generators = [new SignalTypeGenerator()];
    private readonly IReadOnlyCollection<Assembly> assembliesToLoad = [typeof(ISignalIdFactory).Assembly];

    [Test]
    public Task GivenTestSignalInGlobalNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

[Signal]
public partial record TestSignal;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignal_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[Signal]
public partial record TestSignal;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalWithMultiplePartials_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[Signal]
public partial record TestSignal(int Payload);

public partial record TestSignal : ISignal<TestSignal>;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenGenericTestSignal_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public interface ITest;

[Signal]
public partial record TestSignal<TPayload>(TPayload Payload)
    where TPayload : ITest;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenMultiGenericTestSignal_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public interface ITest;

[Signal]
public partial record TestSignal<TPayload, TPayload2>(TPayload Payload, TPayload2 Payload2)
    where TPayload : ITest
    where TPayload2 : notnull;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalInNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [Signal]
    public partial record TestSignal;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenPrivateTestSignalInNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [Signal]
    private partial record TestSignal;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalHierarchy_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[Signal]
public partial record TestSignal(int Payload);

[Signal]
public partial record TestSignalSub(int Payload) : TestSignal(Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalWithAlreadyDefinedHandlerInterface_WhenRunningGenerator_SkipsSignalType()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [Signal]
    public partial record TestSignal
    {
        public static string T { get; } = string.Empty;
    }
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalWithJsonSerializerContextInNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Generator.Tests;

[Signal]
public partial record TestSignal;

internal class TestSignalJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
{
    public override JsonTypeInfo GetTypeInfo(Type type) => throw new NotImplementedException();

    protected override JsonSerializerOptions GeneratedSerializerOptions { get; }

    public static JsonSerializerContext Default => null;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalWithJsonSerializerContextInNamespaceInContainerType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Generator.Tests;

partial class Container
{
    [Signal]
    public partial record TestSignal;

    internal class TestSignalJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
    {
        public override JsonTypeInfo GetTypeInfo(Type type) => throw new NotImplementedException();

        protected override JsonSerializerOptions GeneratedSerializerOptions { get; }

        public static JsonSerializerContext Default => null;
    }
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalForTransport_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Signalling;
using System;

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
public partial record TestSignal;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestSignalForMultipleTransports_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Signalling;
using System;

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
public partial record TestSignal;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    private static VerifySettings Settings()
    {
        var settings = new VerifySettings();
        settings.ScrubLinesWithReplace(line => line.ReplaceGeneratorVersion());
        settings.UseDirectory("Snapshots");
        settings.UseTypeName(nameof(SignalTypeGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
