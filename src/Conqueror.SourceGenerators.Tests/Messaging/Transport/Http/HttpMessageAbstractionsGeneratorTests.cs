using Conqueror.SourceGenerators.Messaging.Transport.Http;

namespace Conqueror.SourceGenerators.Tests.Messaging.Transport.Http;

[TestFixture]
public sealed class HttpMessageAbstractionsGeneratorTests
{
    [Test]
    public Task GivenTestMessageWithResponseBothInGlobalNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

[HttpMessage]
public partial record TestMessage : IMessage<TestMessageResponse>, IMessageTypes<TestMessage, TestMessageResponse>
{
    public required int Payload { get; init; }

    static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageResponse>.TypeInjectors
        => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

    static TestMessage IMessageTypes<TestMessage, TestMessageResponse>.EmptyInstance => null;
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseBothInSameNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[HttpMessage]
public partial record TestMessage : IMessage<TestMessageResponse>, IMessageTypes<TestMessage, TestMessageResponse>
{
    public required int Payload { get; init; }

    static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageResponse>.TypeInjectors
        => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

    static TestMessage IMessageTypes<TestMessage, TestMessageResponse>.EmptyInstance => null;
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseBothInSameNamespaceInSameNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

public sealed partial class Container
{
    [HttpMessage]
    public partial record TestMessage : IMessage<TestMessageResponse>, IMessageTypes<TestMessage, TestMessageResponse>
    {
        public required int Payload { get; init; }

        static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageResponse>.TypeInjectors
            => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

        static TestMessage IMessageTypes<TestMessage, TestMessageResponse>.EmptyInstance => null;
    }

    public record TestMessageResponse(int Payload);
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithoutResponseInNamespaceInNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

public sealed partial class Container
{
    [HttpMessage]
    public partial record TestMessageWithoutResponse : IMessage, IMessageTypes<TestMessageWithoutResponse, UnitMessageResponse>
    {
        public required int Payload { get; init; }

        static IReadOnlyCollection<IMessageTypesInjector> IMessage<UnitMessageResponse>.TypeInjectors
            => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessageWithoutResponse>();

        static TestMessageWithoutResponse IMessageTypes<TestMessageWithoutResponse, UnitMessageResponse>.EmptyInstance => null;
    }
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
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
    public partial record TestMessage(int Payload) : IMessage<TestMessageResponse>, IHttpMessage<TestMessage, TestMessageResponse>, IMessageTypes<TestMessage, TestMessageResponse>
    {
        public static string ToQueryString(TestMessage message) => throw new System.NotSupportedException();

        public static TestMessage FromQueryString(string queryString) => throw new System.NotSupportedException();

        static IHttpMessageTypesInjector IHttpMessage.HttpMessageTypesInjector
            => HttpMessageTypesInjector<TestMessage, TestMessageResponse>.Default;

        static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageResponse>.TypeInjectors
            => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

        static TestMessage IMessageTypes<TestMessage, TestMessageResponse>.EmptyInstance => null;
    }

    public record TestMessageResponse(int Payload);
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseWithCustomMethod_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[HttpMessage(HttpMethod = ""DELETE"")]
public partial record TestMessage : IMessage<TestMessageResponse>, IMessageTypes<TestMessage, TestMessageResponse>
{
    public required int Payload { get; init; }

    static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageResponse>.TypeInjectors
        => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

    static TestMessage IMessageTypes<TestMessage, TestMessageResponse>.EmptyInstance => null;
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseWithGETMethod_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[HttpMessage(HttpMethod = ""GET"")]
public partial record TestMessage : IMessage<TestMessageResponse>, IMessageTypes<TestMessage, TestMessageResponse>
{
    public required int Payload { get; init; }

    static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageResponse>.TypeInjectors
        => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

    static TestMessage IMessageTypes<TestMessage, TestMessageResponse>.EmptyInstance => null;
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseWithCustomPath_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[HttpMessage(Path = ""/customPath"")]
public partial record TestMessage : IMessage<TestMessageResponse>, IMessageTypes<TestMessage, TestMessageResponse>
{
    public required int Payload { get; init; }

    static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageResponse>.TypeInjectors
        => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

    static TestMessage IMessageTypes<TestMessage, TestMessageResponse>.EmptyInstance => null;
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseWithoutPayload_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[HttpMessage]
public partial record TestMessageWithoutPayload : IMessage<TestMessageResponse>, IMessageTypes<TestMessageWithoutPayload, TestMessageResponse>
{
    static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageResponse>.TypeInjectors
        => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessageWithoutPayload>();

    static TestMessageWithoutPayload IMessageTypes<TestMessageWithoutPayload, TestMessageResponse>.EmptyInstance => null;
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithoutResponseWithoutPayload_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[HttpMessage]
public partial record TestMessageWithoutPayload : IMessage, IMessageTypes<TestMessageWithoutPayload, UnitMessageResponse>
{
    static IReadOnlyCollection<IMessageTypesInjector> IMessage<UnitMessageResponse>.TypeInjectors
        => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessageWithoutPayload>();

    static TestMessageWithoutPayload IMessageTypes<TestMessageWithoutPayload, UnitMessageResponse>.EmptyInstance => null;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithMultipleProperties_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;

namespace Generator.Tests;

[HttpMessage]
public partial record TestMessage : IMessage<TestMessageResponse>, IMessageTypes<TestMessage, TestMessageResponse>
{
    public required int IntProp { get; init; }
    public required string StringProp { get; init; }
    public required int[] IntArrayProp { get; init; }
    public required List<decimal> DecimalListProp { get; init; }

    static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageResponse>.TypeInjectors
        => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

    static TestMessage IMessageTypes<TestMessage, TestMessageResponse>.EmptyInstance => null;
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithAttributeTypeAlias_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System.Collections.Generic;
using CustomHttpMessageAttribute = Conqueror.HttpMessageAttribute;

namespace Generator.Tests;

[CustomHttpMessageAttribute]
public partial record TestMessage : IMessage<TestMessageResponse>, IMessageTypes<TestMessage, TestMessageResponse>
{
    public required int Payload { get; init; }

    static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageResponse>.TypeInjectors
        => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

    static TestMessage IMessageTypes<TestMessage, TestMessageResponse>.EmptyInstance => null;
}

public record TestMessageResponse(int Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new HttpMessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    private VerifySettings Settings()
    {
        var settings = new VerifySettings();
        settings.ScrubExpectedChanges();
        settings.UseDirectory("Snapshots");
        settings.UseTypeName(nameof(HttpMessageAbstractionsGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
