using System.Reflection;
using System.Text.Json.Serialization;
using Conqueror.SourceGenerators.Eventing;
using Conqueror.SourceGenerators.TestUtil;
using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Tests.Eventing;

[TestFixture]
public sealed class EventingAbstractionsGeneratorTests
{
    private readonly IReadOnlyCollection<IIncrementalGenerator> generators = [new EventingAbstractionsGenerator()];
    private readonly IReadOnlyCollection<Assembly> assembliesToLoad = [typeof(IEventNotificationIdFactory).Assembly];

    [Test]
    public Task GivenTestEventNotificationInGlobalNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

[EventNotification]
public partial record TestEventNotification;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestEventNotification_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[EventNotification]
public partial record TestEventNotification;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestEventNotificationWithMultiplePartials_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[EventNotification]
public partial record TestEventNotification(int Payload);

public partial record TestEventNotification : IEventNotification<TestEventNotification>;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenGenericTestEventNotification_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public interface ITest;

[EventNotification]
public partial record TestEventNotification<TPayload>(TPayload Payload)
    where TPayload : ITest;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenMultiGenericTestEventNotification_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public interface ITest;

[EventNotification]
public partial record TestEventNotification<TPayload, TPayload2>(TPayload Payload, TPayload2 Payload2)
    where TPayload : ITest
    where TPayload2 : notnull;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestEventNotificationInNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [EventNotification]
    public partial record TestEventNotification;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenPrivateTestEventNotificationInNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [EventNotification]
    private partial record TestEventNotification;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestEventNotificationHierarchy_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[EventNotification]
public partial record TestEventNotification(int Payload);

[EventNotification]
public partial record TestEventNotificationSub(int Payload) : TestEventNotification(Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestEventNotificationWithAlreadyDefinedHandlerInterface_WhenRunningGenerator_SkipsNotificationType()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [EventNotification]
    public partial record TestEventNotification
    {
        public static string T { get; } = string.Empty;
    }
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestEventNotificationWithJsonSerializerContextInNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Generator.Tests;

[EventNotification]
public partial record TestEventNotification;

internal class TestEventNotificationJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
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
    public Task GivenTestEventNotificationWithJsonSerializerContextInNamespaceInContainerType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Generator.Tests;

partial class Container
{
    [EventNotification]
    public partial record TestEventNotification;

    internal class TestEventNotificationJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
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
        settings.UseTypeName(nameof(EventingAbstractionsGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
