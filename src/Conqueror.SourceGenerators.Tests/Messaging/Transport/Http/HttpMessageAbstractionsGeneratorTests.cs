using System.Reflection;
using Conqueror.SourceGenerators.Messaging;
using Conqueror.SourceGenerators.Messaging.Transport.Http;
using Conqueror.SourceGenerators.TestUtil;
using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Tests.Messaging.Transport.Http;

[TestFixture]
public sealed class HttpMessageAbstractionsGeneratorTests
{
    private readonly IReadOnlyCollection<IIncrementalGenerator> generators = [new MessageAbstractionsGenerator(), new HttpMessageAbstractionsGenerator()];
    private readonly IReadOnlyCollection<Assembly> assembliesToLoad = [typeof(UnitMessageResponse).Assembly, typeof(IHttpMessage).Assembly];

    [Test]
    public Task GivenTestMessageWithResponseBothInGlobalNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

[HttpMessage]
[Message<TestMessageResponse>]
public partial record TestMessage
{
    public required int Payload { get; init; }
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseBothInSameNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[HttpMessage]
[Message<TestMessageResponse>]
public partial record TestMessage
{
    public required int Payload { get; init; }
}

public record TestMessageResponse(int Payload);";

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
    [HttpMessage]
    [Message<TestMessageResponse>]
    public partial record TestMessage
    {
        public required int Payload { get; init; }
    }

    public record TestMessageResponse(int Payload);
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
    [HttpMessage]
    [Message]
    public partial record TestMessageWithoutResponse
    {
        public required int Payload { get; init; }
    }
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithAlreadyDefinedMessageInterface_WhenRunningGenerator_SkipsMessageType()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

public sealed partial class Container
{
    [HttpMessage]
    [Message<TestMessageResponse>]
    public partial record TestMessage(int Payload) : IHttpMessage<TestMessage, TestMessageResponse>
    {
        public interface IHandler;

        public static string ToQueryString(TestMessage message) => throw new System.NotSupportedException();

        public static TestMessage FromQueryString(string queryString) => throw new System.NotSupportedException();

        static IHttpMessageTypesInjector IHttpMessage.HttpMessageTypesInjector
            => HttpMessageTypesInjector<TestMessage, TestMessageResponse>.Default;

        public static MessageTypes<TestMessage, TestMessageResponse> T => MessageTypes<TestMessage, TestMessageResponse>.Default;

        static IDefaultMessageTypesInjector IMessage<TestMessage, TestMessageResponse>.DefaultTypeInjector
            => throw new System.NotSupportedException();

        static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessage, TestMessageResponse>.TypeInjectors
            => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

        static TestMessage IMessage<TestMessage, TestMessageResponse>.EmptyInstance => null;
    }

    public record TestMessageResponse(int Payload);
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseWithCustomMethod_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[HttpMessage(HttpMethod = ""DELETE"")]
[Message<TestMessageResponse>]
public partial record TestMessage
{
    public required int Payload { get; init; }
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseWithGETMethod_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[HttpMessage(HttpMethod = ""GET"")]
[Message<TestMessageResponse>]
public partial record TestMessage
{
    public required int Payload { get; init; }
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseWithCustomPath_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[HttpMessage(Path = ""/customPath"")]
[Message<TestMessageResponse>]
public partial record TestMessage
{
    public required int Payload { get; init; }
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseWithoutPayload_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[HttpMessage]
[Message<TestMessageResponse>]
public partial record TestMessageWithoutPayload;

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithoutResponseWithoutPayload_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[HttpMessage]
[Message]
public partial record TestMessageWithoutPayload;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithMultipleProperties_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[HttpMessage]
[Message<TestMessageResponse>]
public partial record TestMessage
{
    public required int IntProp { get; init; }
    public required string StringProp { get; init; }
    public required int[] IntArrayProp { get; init; }
    public required List<decimal> DecimalListProp { get; init; }
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithAttributeTypeAlias_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using CustomHttpMessageAttribute = Conqueror.HttpMessageAttribute;

namespace Generator.Tests;

[CustomHttpMessageAttribute]
[Message<TestMessageResponse>]
public partial record TestMessage
{
    public required int Payload { get; init; }
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    private VerifySettings Settings()
    {
        var settings = new VerifySettings();
        settings.ScrubLinesWithReplace(line => line.ReplaceGeneratorVersion());
        settings.UseDirectory("Snapshots");
        settings.UseTypeName(nameof(HttpMessageAbstractionsGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
