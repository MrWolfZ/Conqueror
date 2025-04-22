using System.Reflection;
using System.Text.Json.Serialization;
using Conqueror.SourceGenerators.Signalling;
using Conqueror.SourceGenerators.TestUtil;
using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Tests.Signalling;

[TestFixture]
public sealed class SignallingAbstractionsGeneratorTests
{
    private readonly IReadOnlyCollection<IIncrementalGenerator> generators = [new SignallingAbstractionsGenerator()];
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

    private static VerifySettings Settings()
    {
        var settings = new VerifySettings();
        settings.ScrubLinesWithReplace(line => line.ReplaceGeneratorVersion());
        settings.UseDirectory("Snapshots");
        settings.UseTypeName(nameof(SignallingAbstractionsGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
