using System.Reflection;
using System.Text.Json.Serialization;
using Conqueror.SourceGenerators.Messaging;
using Conqueror.SourceGenerators.TestUtil;
using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Tests.Messaging;

[TestFixture]
public sealed class MessageAbstractionsGeneratorTests
{
    private readonly IReadOnlyCollection<IIncrementalGenerator> generators = [new MessageAbstractionsGenerator()];
    private readonly IReadOnlyCollection<Assembly> assembliesToLoad = [typeof(UnitMessageResponse).Assembly];

    [Test]
    public Task GivenTestMessageWithResponseBothInGlobalNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

[Message<TestMessageResponse>]
public partial record TestMessage;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseBothInSameNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[Message<TestMessageResponse>]
public partial record TestMessage;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseBothInSameNamespaceWithMultiplePartials_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[Message<TestMessageResponse>]
public partial record TestMessage(int Payload);

public partial record TestMessage : IMessage<TestMessage, TestMessageResponse>;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseBothInSameNamespaceInSameNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [Message<TestMessageResponse>]
    public partial record TestMessage;

    public record TestMessageResponse;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithoutResponseInNamespaceInNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [Message]
    public partial record TestMessageWithoutResponse;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithAlreadyDefinedHandlerInterface_WhenRunningGenerator_SkipsMessageType()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [Message<TestMessageResponse>]
    public partial record TestMessage
    {
        public interface IHandler;
    }

    public record TestMessageResponse;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithArrayResponseBothInSameNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[Message<TestMessageResponse[]>]
public partial record TestMessage;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithListResponseBothInSameNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[Message<List<TestMessageResponse>>]
public partial record TestMessage;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithEnumerableResponseBothInSameNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[Message<IEnumerable<TestMessageResponse>>]
public partial record TestMessage;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithJsonSerializerContextInSameNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Generator.Tests;

[Message<TestMessageResponse>]
public partial record TestMessage;

public record TestMessageResponse;

internal class TestMessageJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
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
    public Task GivenTestMessageWithJsonSerializerContextInSameNamespaceInContainerType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Generator.Tests;

partial class Container
{
    [Message<TestMessageResponse>]
    public partial record TestMessage;

    public record TestMessageResponse;

    internal class TestMessageJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
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

    private VerifySettings Settings()
    {
        var settings = new VerifySettings();
        settings.ScrubLinesWithReplace(line => line.ReplaceGeneratorVersion());
        settings.UseDirectory("Snapshots");
        settings.UseTypeName(nameof(MessageAbstractionsGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
