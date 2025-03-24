using Conqueror.SourceGenerators.Messaging;

namespace Conqueror.SourceGenerators.Tests.Messaging;

[TestFixture]
public sealed class MessageAbstractionsGeneratorTests
{
    [Test]
    public Task GivenTestMessageWithResponseBothInGlobalNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

[Message<TestMessageResponse>]
public partial record TestMessage;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

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

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

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

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

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

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

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

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

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

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty, output);
        return Verify(output, Settings());
    }

    private VerifySettings Settings()
    {
        var settings = new VerifySettings();
        settings.ScrubExpectedChanges();
        settings.UseDirectory("Snapshots");
        settings.UseTypeName(nameof(MessageAbstractionsGeneratorTests));
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }
}
