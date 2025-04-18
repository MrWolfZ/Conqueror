using System.Reflection;
using Conqueror.SourceGenerators.TestUtil;
using Conqueror.Transport.Http.SourceGenerators.Messaging;
using Microsoft.CodeAnalysis;

namespace Conqueror.Transport.Http.SourceGenerators.Tests.Messaging;

[TestFixture]
public sealed class HttpMessagingAbstractionsGeneratorTests
{
    private readonly IReadOnlyCollection<IIncrementalGenerator> generators = [new HttpMessagingAbstractionsGenerator()];
    private readonly IReadOnlyCollection<Assembly> assembliesToLoad = [typeof(UnitMessageResponse).Assembly, typeof(IHttpMessage).Assembly];

    [Test]
    public Task GivenTestMessageWithResponseBothInGlobalNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

[HttpMessage<TestMessageResponse>]
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

[HttpMessage<TestMessageResponse>]
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
    [HttpMessage<TestMessageResponse>]
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
    [HttpMessage<TestMessageResponse>]
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

[HttpMessage<TestMessageResponse>(HttpMethod = ""DELETE"")]
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

[HttpMessage<TestMessageResponse>(HttpMethod = ""GET"")]
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

[HttpMessage<TestMessageResponse>(Path = ""/customPath"")]
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

[HttpMessage<TestMessageResponse>]
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

[HttpMessage<TestMessageResponse>]
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
        const string input = @"using CustomHttpMessageAttribute = Conqueror.HttpMessageAttribute;

namespace Generator.Tests;

[CustomHttpMessageAttribute]
public partial record TestMessage
{
    public required int Payload { get; init; }
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithHttpAndCoreAttribute_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[Message<TestMessageResponse>]
[HttpMessage<TestMessageResponse>]
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
    public Task GivenTestMessageWithoutResponseWithHttpAndCoreAttribute_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[Message]
[HttpMessage]
public partial record TestMessage
{
    public required int Payload { get; init; }
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

[HttpMessage<TestMessageResponse[]>]
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
    public Task GivenTestMessageWithListResponseBothInSameNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[HttpMessage<List<TestMessageResponse>>]
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
    public Task GivenTestMessageWithEnumerableResponseBothInSameNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[HttpMessage<IEnumerable<TestMessageResponse>>]
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
        settings.UseTypeName(nameof(HttpMessagingAbstractionsGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
