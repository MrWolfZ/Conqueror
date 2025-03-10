namespace Conqueror.SourceGenerators.Tests;

[TestFixture]
public sealed class MessageAbstractionsGeneratorTests
{
    [Test]
    public Task GivenTestMessageWithResponseBothInGlobalNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

public partial record TestMessage : IMessage<TestMessageResponse>;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseBothInSameNamespace_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public partial record TestMessage : IMessage<TestMessageResponse>;

public record TestMessageResponse;";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithResponseBothInSameNamespaceInSameNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    public partial record TestMessage : IMessage<TestMessageResponse>;

    public record TestMessageResponse;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithoutResponseInNamespaceInNestedType_WhenRunningGenerator_GeneratesCorrectTypes()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    public partial record TestMessageWithoutResponse : IMessage;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    [Test]
    public Task GivenTestMessageWithAlreadyDefinedHandlerInterface_WhenRunningGenerator_SkipsMessageType()
    {
        const string input = @"using Conqueror;

namespace Generator.Tests;

public sealed partial class Container
{
    public partial record TestMessage : IMessage<TestMessageResponse>
    {
        public interface IHandler;
    }

    public record TestMessageResponse;
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageAbstractionsGenerator()], new(input));

        Assert.That(diagnostics, Is.Empty);
        return Verify(output, Settings());
    }

    private VerifySettings Settings()
    {
        var settings = new VerifySettings();
        settings.ScrubExpectedChanges();
        settings.UseDirectory("Snapshots");
        settings.UseTypeName(nameof(MessageAbstractionsGeneratorTests));
        settings.DisableRequireUniquePrefix();

        return settings;
    }
}
