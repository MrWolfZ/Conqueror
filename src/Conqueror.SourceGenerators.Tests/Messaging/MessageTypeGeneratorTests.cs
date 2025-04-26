using System.Reflection;
using System.Text.Json.Serialization;
using Conqueror.SourceGenerators.Messaging;
using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Tests.Messaging;

[TestFixture]
public sealed class MessageTypeGeneratorTests
{
    private readonly IReadOnlyCollection<IIncrementalGenerator> generators = [new MessageTypeGenerator()];
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
    public Task GivenPrivateTestMessageWithResponseBothInSameNamespaceInSameNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [Message<TestMessageResponse>]
    private partial record TestMessage;

    private record TestMessageResponse;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenPrivateTestMessageWithoutResponseBothInSameNamespaceInSameNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [Message]
    private partial record TestMessage;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenGenericTestMessage_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public interface ITest;

[Message<TestMessageResponse>]
public partial record TestMessage<TPayload>(TPayload Payload)
    where TPayload : ITest;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenMultiGenericTestMessage_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public interface ITest;

[Message<TestMessageResponse>]
public partial record TestMessage<TPayload, TPayload2>(TPayload Payload, TPayload2 Payload2)
    where TPayload : ITest
    where TPayload2 : notnull;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenGenericTestMessageWithoutResponse_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public interface ITest;

[Message]
public partial record TestMessage<TPayload>(TPayload Payload)
    where TPayload : ITest;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessagesWithHierarchy_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[Message<TestMessageResponse>]
public partial record TestMessage(int Payload);

[Message<TestMessageResponse>]
public partial record TestMessageSub(int Payload) : TestMessage(Payload);

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessagesWithoutResponseWithHierarchy_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

[Message]
public partial record TestMessage(int Payload);

[Message]
public partial record TestMessageSub(int Payload) : TestMessage(Payload);";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad.Concat([typeof(JsonSerializerContext).Assembly]), new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithInconsistentResponseTypes_WhenRunningGenerator_ProducesDiagnosticsAndGeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using Conqueror.Messaging;
using System;

namespace Generator.Tests;

[MessageTransport(Prefix = ""Core"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DuplicateMessageAttribute<TResponse> : Attribute;

[Message<TestMessageResponse>]
[DuplicateMessage<TestMessageResponse2>]
public partial record TestMessage;

public record TestMessageResponse;

public record TestMessageResponse2;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Not.Empty, output);
        return Verify(output + "\n\nDiagnostics:\n" + string.Join("\n", diagnostics), Settings());
    }

    [Test]
    public Task GivenTestMessageWithAlreadyDefinedTProperty_WhenRunningGenerator_SkipsMessageType()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    [Message<TestMessageResponse>]
    public partial record TestMessage
    {
        public static string T { get; } = string.Empty;
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

    [Test]
    public Task GivenTestMessageForTransport_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Messaging;
using System;

namespace Generator.Tests;

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute : Attribute
{
    public string? StringProperty { get; init; }

    public int IntProperty { get; init; }

    public int[]? IntArrayProperty { get; init; }

    public string? NullProperty { get; init; }

    public string? UnsetProperty { get; init; }
}

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute<TResponse> : Attribute
{
    public string? StringProperty { get; init; }

    public int IntProperty { get; init; }

    public int[]? IntArrayProperty { get; init; }

    public string? NullProperty { get; init; }

    public string? UnsetProperty { get; init; }
}

public interface ITestTransportMessage<TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
{
    static virtual string StringProperty { get; set; } = ""Default"";

    static virtual int IntProperty { get; set; }

    static virtual int[] IntArrayProperty { get; set; } = [];

    static virtual string? NullProperty { get; set; }

    static virtual string? UnsetProperty { get; set; }
}

public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
{
    static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

[TestTransportMessage<TestMessageResponse>(StringProperty = ""Test"", IntProperty = 1, IntArrayProperty = new[] { 1, 2, 3 }, NullProperty = null)]
public partial record TestMessage;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithoutResponseForTransport_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Messaging;
using System;

namespace Generator.Tests;

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute : Attribute
{
    public string? StringProperty { get; init; }

    public int IntProperty { get; init; }

    public int[]? IntArrayProperty { get; init; }

    public string? NullProperty { get; init; }

    public string? UnsetProperty { get; init; }
}

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute<TResponse> : Attribute
{
    public string? StringProperty { get; init; }

    public int IntProperty { get; init; }

    public int[]? IntArrayProperty { get; init; }

    public string? NullProperty { get; init; }

    public string? UnsetProperty { get; init; }
}

public interface ITestTransportMessage<TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
{
    static virtual string StringProperty { get; set; } = ""Default"";

    static virtual int IntProperty { get; set; }

    static virtual int[] IntArrayProperty { get; set; } = [];

    static virtual string? NullProperty { get; set; }

    static virtual string? UnsetProperty { get; set; }
}

public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
{
    static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

[TestTransportMessage(StringProperty = ""Test"", IntProperty = 1, IntArrayProperty = new[] { 1, 2, 3 }, NullProperty = null)]
public partial record TestMessageWithoutResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageForMultipleTransports_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Messaging;
using System;

namespace Generator.Tests;

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute : Attribute
{
    public string? StringProperty { get; init; }
}

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute<TResponse> : Attribute
{
    public string? StringProperty { get; init; }
}

[MessageTransport(Prefix = ""TestTransport2"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2MessageAttribute : Attribute;

[MessageTransport(Prefix = ""TestTransport2"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2MessageAttribute<TResponse> : Attribute;

public interface ITestTransportMessage<TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
{
    static virtual string StringProperty { get; set; } = ""Default"";
}

public interface ITestTransport2Message<TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, ITestTransport2Message<TMessage, TResponse>
{
    static virtual string? StringProperty { get; set; }
}

public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
{
    static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

public interface ITestTransport2MessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, ITestTransport2Message<TMessage, TResponse>
    where TIHandler : class, ITestTransport2MessageHandler<TMessage, TResponse, TIHandler>
{
    static IMessageHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

[Message<TestMessageResponse>]
[TestTransportMessage<TestMessageResponse>(StringProperty = ""Test"")]
[TestTransport2Message<TestMessageResponse>]
public partial record TestMessage;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithoutResponseForMultipleTransports_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Messaging;
using System;

namespace Generator.Tests;

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute : Attribute
{
    public string? StringProperty { get; init; }
}

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute<TResponse> : Attribute
{
    public string? StringProperty { get; init; }
}

[MessageTransport(Prefix = ""TestTransport2"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2MessageAttribute : Attribute;

[MessageTransport(Prefix = ""TestTransport2"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2MessageAttribute<TResponse> : Attribute;

public interface ITestTransportMessage<TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
{
    static virtual string StringProperty { get; set; } = ""Default"";
}

public interface ITestTransport2Message<TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, ITestTransport2Message<TMessage, TResponse>
{
    static virtual string? StringProperty { get; set; }
}

public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
{
    static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

public interface ITestTransport2MessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, ITestTransport2Message<TMessage, TResponse>
    where TIHandler : class, ITestTransport2MessageHandler<TMessage, TResponse, TIHandler>
{
    static IMessageHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotImplementedException();
}

[Message]
[TestTransportMessage(StringProperty = ""Test"")]
[TestTransport2Message]
public partial record TestMessageWithoutResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageForTransportWithMessageTypeOverride_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Messaging;
using System;

namespace Generator.OriginalTransport
{
    [MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.OriginalTransport"")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportMessageAttribute : Attribute
    {
        public string? StringProperty { get; init; }
    }

    [MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.OriginalTransport"")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportMessageAttribute<TResponse> : Attribute
    {
        public string? StringProperty { get; init; }
    }

    public interface ITestTransportMessage<TMessage, TResponse> : IMessage<TMessage, TResponse>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    {
        static virtual string StringProperty { get; set; } = ""Default"";
    }

    public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>
        where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    {
        static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
            where THandler : class, TIHandler
            => throw new NotImplementedException();
    }
}

namespace Generator.CustomTransport
{
    [MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.OriginalTransport"",
                      FullyQualifiedMessageTypeName = ""Generator.CustomTransport.ICustomTestTransportMessage"")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CustomTestTransportMessageAttribute : Attribute
    {
        public string? ExtraProperty { get; init; }
    }

    [MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.OriginalTransport"",
                      FullyQualifiedMessageTypeName = ""Generator.CustomTransport.ICustomTestTransportMessage"")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CustomTestTransportMessageAttribute<TResponse> : Attribute
    {
        public string? ExtraProperty { get; init; }
    }

    public interface ICustomTestTransportMessage<TMessage, TResponse> : Generator.OriginalTransport.ITestTransportMessage<TMessage, TResponse>
        where TMessage : class, ICustomTestTransportMessage<TMessage, TResponse>
    {
        static string Generator.OriginalTransport.ITestTransportMessage<TMessage, TResponse>.StringProperty { get; set; } = TMessage.ExtraProperty ?? ""Default"";

        static virtual string? ExtraProperty { get; set; }
    }
}

namespace Generator.Tests
{
    using Generator.CustomTransport;

    [CustomTestTransportMessage<TestMessageResponse>(ExtraProperty = ""Test"")]
    public partial record TestMessage;

    public record TestMessageResponse;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTypeWithAttributeWithSameSuffix_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using System;

namespace Generator.Tests;

public class MyMessageAttribute : Attribute;

[MyMessage]
public partial record TestMessage;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    private static VerifySettings Settings()
    {
        var settings = new VerifySettings();
        settings.ScrubLinesWithReplace(line => line.ReplaceGeneratorVersion());
        settings.UseDirectory("Snapshots");
        settings.UseTypeName(nameof(MessageTypeGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
