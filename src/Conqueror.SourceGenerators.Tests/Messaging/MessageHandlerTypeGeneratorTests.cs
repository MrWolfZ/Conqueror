using System.Reflection;
using Conqueror.SourceGenerators.Messaging;
using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Tests.Messaging;

[TestFixture]
public sealed class MessageHandlerTypeGeneratorTests
{
    private readonly IReadOnlyCollection<IIncrementalGenerator> generators = [new MessageTypeGenerator(), new MessageHandlerTypeGenerator()];
    private readonly IReadOnlyCollection<Assembly> assembliesToLoad = [typeof(IMessageIdFactory).Assembly];

    [Test]
    public Task GivenTestMessageHandler_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[Message<TestMessageResponse>]
public partial record TestMessage;

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithoutResponseHandler_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[Message]
public partial record TestMessageWithoutResponse;

public partial class TestMessageWithoutResponseHandler : TestMessageWithoutResponse.IHandler
{
    public Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageHandlerForMultipleMessageTypes_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[Message<TestMessageResponse>]
public partial record TestMessage;

[Message<TestMessageResponse>]
public partial record TestMessage2;

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler,
                                          TestMessage2.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<TestMessageResponse> Handle(TestMessage2 message, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output + "\n\nDiagnostics:\n" + string.Join("\n", diagnostics), Settings());
    }

    [Test]
    public Task GivenTestMessageHandlerForMultipleMessageTypesWithoutResponse_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[Message]
public partial record TestMessage;

[Message]
public partial record TestMessage2;

public partial class TestMessageHandler : TestMessage.IHandler,
                                          TestMessage2.IHandler
{
    public Task Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task Handle(TestMessage2 message, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output + "\n\nDiagnostics:\n" + string.Join("\n", diagnostics), Settings());
    }

    [Test]
    public Task GivenTestMessageHandlerForMessageWithInconsistentResponseTypes_WhenRunningGenerator_ProducesDiagnosticsAndGeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using Conqueror.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[MessageTransport(Prefix = ""Core"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DuplicateMessageAttribute<TResponse> : Attribute;

[Message<TestMessageResponse>]
[DuplicateMessage<TestMessageResponse2>]
public partial record TestMessage;

public record TestMessageResponse;

public record TestMessageResponse2;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Not.Empty, output);
        return Verify(output + "\n\nDiagnostics:\n" + string.Join("\n", diagnostics), Settings());
    }

    [Test]
    public Task GivenTestMessageHandlerWithImplementedGetTypeInjectorsMethod_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[Message<TestMessageResponse>]
public partial record TestMessage;

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();

    static IEnumerable<IMessageHandlerTypesInjector> IMessageHandler.GetTypeInjectors() => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageHandlerForTransport_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TestTransportMessageAttribute : Attribute
{
    public string? StringProperty { get; init; }

    public int IntProperty { get; init; }

    public int[]? IntArrayProperty { get; init; }

    public string? NullProperty { get; init; }

    public string? UnsetProperty { get; init; }
}

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

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

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithoutResponseHandlerForTransport_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TestTransportMessageAttribute : Attribute
{
    public string? StringProperty { get; init; }

    public int IntProperty { get; init; }

    public int[]? IntArrayProperty { get; init; }

    public string? NullProperty { get; init; }

    public string? UnsetProperty { get; init; }
}

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

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
public partial record TestMessage;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageHandlerForMultipleTransports_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TestTransportMessageAttribute : Attribute
{
    public string? StringProperty { get; init; }
}

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

[MessageTransport(Prefix = ""TestTransport2"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TestTransport2MessageAttribute : Attribute;

[MessageTransport(Prefix = ""TestTransport2"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2MessageAttribute<TResponse> : TestTransport2MessageAttribute;

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

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(generators, assembliesToLoad, new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithoutResponseHandlerForMultipleTransports_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"#nullable enable

using Conqueror;
using Conqueror.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Generator.Tests;

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TestTransportMessageAttribute : Attribute
{
    public string? StringProperty { get; init; }
}

[MessageTransport(Prefix = ""TestTransport"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

[MessageTransport(Prefix = ""TestTransport2"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TestTransport2MessageAttribute : Attribute;

[MessageTransport(Prefix = ""TestTransport2"", Namespace = ""Generator.Tests"")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2MessageAttribute<TResponse> : TestTransport2MessageAttribute;

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
public partial record TestMessage;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotImplementedException();
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
        settings.UseTypeName(nameof(MessageHandlerTypeGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
