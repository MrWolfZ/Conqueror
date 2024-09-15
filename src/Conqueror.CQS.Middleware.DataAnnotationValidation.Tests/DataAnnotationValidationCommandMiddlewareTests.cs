using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Hosting;

namespace Conqueror.CQS.Middleware.DataAnnotationValidation.Tests;

[TestFixture]
public sealed class DataAnnotationValidationCommandMiddlewareTests
{
    private sealed class TestCommandClassWithoutValidationAnnotations
    {
        public string? Payload { get; set; }
    }

    [Test]
    public void GivenCommandClassWithoutValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithoutValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithoutValidationAnnotations>(host);

        var testCommand = new TestCommandClassWithoutValidationAnnotations { Payload = "test" };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    private sealed record TestCommandRecordWithoutValidationAnnotations(string? Payload);

    [Test]
    public void GivenCommandRecordWithoutValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandRecordWithoutValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithoutValidationAnnotations>(host);

        var testCommand = new TestCommandRecordWithoutValidationAnnotations("test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    private sealed class TestCommandClassWithoutPayload;

    [Test]
    public void GivenCommandClassWithoutPayload_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithoutPayload>();
        var handler = CreateHandler<TestCommandClassWithoutPayload>(host);

        var testCommand = new TestCommandClassWithoutPayload();

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    private sealed record TestRecordWithoutPayload;

    [Test]
    public void GivenCommandRecordWithoutPayload_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithoutPayload>();
        var handler = CreateHandler<TestRecordWithoutPayload>(host);

        var testCommand = new TestRecordWithoutPayload();

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    private sealed class TestCommandClassWithSinglePropertyValidationAnnotation
    {
        [Required]
        public string? Payload { get; set; }
    }

    [Test]
    public void GivenValidCommandClassWithSinglePropertyValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithSinglePropertyValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSinglePropertyValidationAnnotation>(host);

        var testCommand = new TestCommandClassWithSinglePropertyValidationAnnotation { Payload = "test" };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithSinglePropertyValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandClassWithSinglePropertyValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSinglePropertyValidationAnnotation>(host);

        var testCommand = new TestCommandClassWithSinglePropertyValidationAnnotation();

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSinglePropertyValidationAnnotation.Payload) }));
    }

    private sealed record TestRecordWithSinglePropertyValidationAnnotation
    {
        [Required]
        public string? Payload { get; set; }
    }

    [Test]
    public void GivenValidCommandRecordWithSinglePropertyValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSinglePropertyValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSinglePropertyValidationAnnotation>(host);

        var testCommand = new TestRecordWithSinglePropertyValidationAnnotation { Payload = "test" };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithSinglePropertyValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSinglePropertyValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSinglePropertyValidationAnnotation>(host);

        var testCommand = new TestRecordWithSinglePropertyValidationAnnotation();

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestRecordWithSinglePropertyValidationAnnotation.Payload) }));
    }

    private sealed record TestRecordWithSinglePropertyValidationAnnotationInConstructor([property: Required] string? Payload);

    [Test]
    public void GivenValidCommandRecordWithSinglePropertyValidationAnnotationInConstructor_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSinglePropertyValidationAnnotationInConstructor>();
        var handler = CreateHandler<TestRecordWithSinglePropertyValidationAnnotationInConstructor>(host);

        var testCommand = new TestRecordWithSinglePropertyValidationAnnotationInConstructor("test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithSinglePropertyValidationAnnotationInConstructor_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSinglePropertyValidationAnnotationInConstructor>();
        var handler = CreateHandler<TestRecordWithSinglePropertyValidationAnnotationInConstructor>(host);

        var testCommand = new TestRecordWithSinglePropertyValidationAnnotationInConstructor(null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestRecordWithSinglePropertyValidationAnnotationInConstructor.Payload) }));
    }

    private sealed class TestCommandClassWithSinglePropertyWithMultipleValidationAnnotations
    {
        [MinLength(2)]
        [MaxLength(5)]
        public string? Payload { get; set; }
    }

    [Test]
    public void GivenValidCommandClassWithSinglePropertyWithMultipleValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithSinglePropertyWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithSinglePropertyWithMultipleValidationAnnotations>(host);

        var testCommand = new TestCommandClassWithSinglePropertyWithMultipleValidationAnnotations { Payload = "test" };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithSinglePropertyWithMultipleValidationAnnotations_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandClassWithSinglePropertyWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithSinglePropertyWithMultipleValidationAnnotations>(host);

        var testCommand1 = new TestCommandClassWithSinglePropertyWithMultipleValidationAnnotations { Payload = "t" };
        var testCommand2 = new TestCommandClassWithSinglePropertyWithMultipleValidationAnnotations { Payload = "too-long" };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSinglePropertyWithMultipleValidationAnnotations.Payload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSinglePropertyWithMultipleValidationAnnotations.Payload) }));
    }

    private sealed record TestCommandRecordWithSinglePropertyWithMultipleValidationAnnotations
    {
        [MinLength(2)]
        [MaxLength(5)]
        public string? Payload { get; set; }
    }

    [Test]
    public void GivenValidCommandRecordWithSinglePropertyWithMultipleValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandRecordWithSinglePropertyWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithSinglePropertyWithMultipleValidationAnnotations>(host);

        var testCommand = new TestCommandRecordWithSinglePropertyWithMultipleValidationAnnotations { Payload = "test" };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithSinglePropertyWithMultipleValidationAnnotations_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandRecordWithSinglePropertyWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithSinglePropertyWithMultipleValidationAnnotations>(host);

        var testCommand1 = new TestCommandRecordWithSinglePropertyWithMultipleValidationAnnotations { Payload = "t" };
        var testCommand2 = new TestCommandRecordWithSinglePropertyWithMultipleValidationAnnotations { Payload = "too-long" };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandRecordWithSinglePropertyWithMultipleValidationAnnotations.Payload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandRecordWithSinglePropertyWithMultipleValidationAnnotations.Payload) }));
    }

    private sealed class TestCommandClassWithSingleConstructorValidationAnnotation([Required] string? payload)
    {
        public string? Payload { get; } = payload;
    }

    [Test]
    public void GivenValidCommandClassWithSingleConstructorValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithSingleConstructorValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSingleConstructorValidationAnnotation>(host);

        var testCommand = new TestCommandClassWithSingleConstructorValidationAnnotation("test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithSingleConstructorValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandClassWithSingleConstructorValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSingleConstructorValidationAnnotation>(host);

        var testCommand = new TestCommandClassWithSingleConstructorValidationAnnotation(null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleConstructorValidationAnnotation.Payload) }));
    }

    private sealed record TestCommandRecordWithSingleConstructorValidationAnnotation([Required] string? Payload);

    [Test]
    public void GivenValidCommandRecordWithSingleConstructorValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandRecordWithSingleConstructorValidationAnnotation>();
        var handler = CreateHandler<TestCommandRecordWithSingleConstructorValidationAnnotation>(host);

        var testCommand = new TestCommandRecordWithSingleConstructorValidationAnnotation("test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithSingleConstructorValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandRecordWithSingleConstructorValidationAnnotation>();
        var handler = CreateHandler<TestCommandRecordWithSingleConstructorValidationAnnotation>(host);

        var testCommand = new TestCommandRecordWithSingleConstructorValidationAnnotation(null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandRecordWithSingleConstructorValidationAnnotation.Payload) }));
    }

    private sealed class TestCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations([MinLength(2)] [MaxLength(5)] string? payload)
    {
        public string? Payload { get; } = payload;
    }

    [Test]
    public void GivenValidCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations>(host);

        var testCommand = new TestCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations("test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations>(host);

        var testCommand1 = new TestCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations("t");
        var testCommand2 = new TestCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations("too-long");

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations.Payload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleConstructorParameterWithMultipleValidationAnnotations.Payload) }));
    }

    private sealed record TestCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations([MinLength(2)] [MaxLength(5)] string? Payload);

    [Test]
    public void GivenValidCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations>(host);

        var testCommand = new TestCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations("test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations>(host);

        var testCommand1 = new TestCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations("t");
        var testCommand2 = new TestCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations("too-long");

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations.Payload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandRecordWithSingleConstructorParameterWithMultipleValidationAnnotations.Payload) }));
    }

    private sealed class TestCommandClassWithMultiplePropertyValidationAnnotations
    {
        [Required]
        public string? Payload1 { get; set; }

        [Required]
        public string? Payload2 { get; set; }
    }

    [Test]
    public void GivenValidCommandClassWithMultiplePropertyValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithMultiplePropertyValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithMultiplePropertyValidationAnnotations>(host);

        var testCommand = new TestCommandClassWithMultiplePropertyValidationAnnotations
        {
            Payload1 = "test",
            Payload2 = "test",
        };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithMultiplePropertyValidationAnnotations_ThrowsValidationExceptionWithMultipleErrors()
    {
        using var host = CreateHost<TestCommandClassWithMultiplePropertyValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithMultiplePropertyValidationAnnotations>(host);

        var testCommand = new TestCommandClassWithMultiplePropertyValidationAnnotations
        {
            Payload1 = null,
            Payload2 = null,
        };

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestCommandClassWithMultiplePropertyValidationAnnotations.Payload1),
            nameof(TestCommandClassWithMultiplePropertyValidationAnnotations.Payload2),
        }));
    }

    private sealed record TestRecordWithMultiplePropertyValidationAnnotations
    {
        [Required]
        public string? Payload1 { get; set; }

        [Required]
        public string? Payload2 { get; set; }
    }

    [Test]
    public void GivenValidCommandRecordWithMultiplePropertyValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithMultiplePropertyValidationAnnotations>();
        var handler = CreateHandler<TestRecordWithMultiplePropertyValidationAnnotations>(host);

        var testCommand = new TestRecordWithMultiplePropertyValidationAnnotations
        {
            Payload1 = "test",
            Payload2 = "test",
        };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithMultiplePropertyValidationAnnotations_ThrowsValidationExceptionWithMultipleErrors()
    {
        using var host = CreateHost<TestRecordWithMultiplePropertyValidationAnnotations>();
        var handler = CreateHandler<TestRecordWithMultiplePropertyValidationAnnotations>(host);

        var testCommand = new TestRecordWithMultiplePropertyValidationAnnotations
        {
            Payload1 = null,
            Payload2 = null,
        };

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestRecordWithMultiplePropertyValidationAnnotations.Payload1),
            nameof(TestRecordWithMultiplePropertyValidationAnnotations.Payload2),
        }));
    }

    private sealed class TestCommandClassWithMultipleConstructorValidationAnnotations([Required] string? payload1, [Required] string? payload2)
    {
        public string? Payload1 { get; } = payload1;

        public string? Payload2 { get; } = payload2;
    }

    [Test]
    public void GivenValidCommandClassWithMultipleConstructorValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithMultipleConstructorValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithMultipleConstructorValidationAnnotations>(host);

        var testCommand = new TestCommandClassWithMultipleConstructorValidationAnnotations("test", "test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithMultipleConstructorValidationAnnotations_ThrowsValidationExceptionWithMultipleErrors()
    {
        using var host = CreateHost<TestCommandClassWithMultipleConstructorValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithMultipleConstructorValidationAnnotations>(host);

        var testCommand = new TestCommandClassWithMultipleConstructorValidationAnnotations(null, null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestCommandClassWithMultipleConstructorValidationAnnotations.Payload1),
            nameof(TestCommandClassWithMultipleConstructorValidationAnnotations.Payload2),
        }));
    }

    private sealed record TestCommandRecordWithMultipleConstructorValidationAnnotations([Required] string? Payload1, [Required] string? Payload2);

    [Test]
    public void GivenValidCommandRecordWithMultipleConstructorValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandRecordWithMultipleConstructorValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithMultipleConstructorValidationAnnotations>(host);

        var testCommand = new TestCommandRecordWithMultipleConstructorValidationAnnotations("test", "test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithMultipleConstructorValidationAnnotations_ThrowsValidationExceptionWithMultipleErrors()
    {
        using var host = CreateHost<TestCommandRecordWithMultipleConstructorValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithMultipleConstructorValidationAnnotations>(host);

        var testCommand = new TestCommandRecordWithMultipleConstructorValidationAnnotations(null, null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestCommandRecordWithMultipleConstructorValidationAnnotations.Payload1),
            nameof(TestCommandRecordWithMultipleConstructorValidationAnnotations.Payload2),
        }));
    }

    private sealed class TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations([Required] string? payload1, [Required] string? payload2)
    {
        public string? Payload1 { get; } = payload1;

        public string? Payload2 { get; } = payload2;

        [Required]
        public string? Payload3 { get; set; }

        [Required]
        public string? Payload4 { get; set; }
    }

    [Test]
    public void GivenValidCommandClassWithMultiplePropertyAndConstructorValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations>(host);

        var testCommand = new TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations("test", "test")
        {
            Payload3 = "test",
            Payload4 = "test",
        };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithMultiplePropertyAndConstructorValidationAnnotations_ThrowsValidationExceptionWithErrorsForAllPropertiesAndConstructorParameters()
    {
        using var host = CreateHost<TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations>(host);

        var testCommand = new TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations(null, null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations.Payload1),
            nameof(TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations.Payload2),
            nameof(TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations.Payload3),
            nameof(TestCommandClassWithMultiplePropertyAndConstructorValidationAnnotations.Payload4),
        }));
    }

    private sealed record TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations([Required] string? Payload1, [Required] string? Payload2, [property: Required] string? Payload3, string? Payload4)
    {
        [Required]
        public string? Payload4 { get; } = Payload4;
    }

    [Test]
    public void GivenValidCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations>(host);

        var testCommand = new TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations("test", "test", "test", "test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations_ThrowsValidationExceptionWithErrorsForAllPropertiesAndConstructorParameters()
    {
        using var host = CreateHost<TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations>(host);

        var testCommand = new TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations(null, null, null, null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations.Payload1),
            nameof(TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations.Payload2),
            nameof(TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations.Payload3),
            nameof(TestCommandRecordWithMultiplePropertyAndConstructorValidationAnnotations.Payload4),
        }));
    }

    private sealed class TestCommandClassWithMultipleConstructorsWithValidationAnnotations
    {
        public TestCommandClassWithMultipleConstructorsWithValidationAnnotations([Required] string? payload1, [Required] string? payload2)
        {
            Payload1 = payload1;
            Payload2 = payload2;
        }

        public TestCommandClassWithMultipleConstructorsWithValidationAnnotations([Required] string? payload3)
        {
            Payload3 = payload3;
        }

        public string? Payload1 { get; }

        public string? Payload2 { get; }

        public string? Payload3 { get; set; }
    }

    [Test]
    public void GivenValidCommandClassWithMultipleConstructorsWithValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithMultipleConstructorsWithValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithMultipleConstructorsWithValidationAnnotations>(host);

        var testCommand = new TestCommandClassWithMultipleConstructorsWithValidationAnnotations("test", "test")
        {
            Payload3 = "test",
        };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithMultipleConstructorsWithValidationAnnotations_ThrowsValidationExceptionWithErrorsForAllConstructorParameters()
    {
        using var host = CreateHost<TestCommandClassWithMultipleConstructorsWithValidationAnnotations>();
        var handler = CreateHandler<TestCommandClassWithMultipleConstructorsWithValidationAnnotations>(host);

        var testCommand1 = new TestCommandClassWithMultipleConstructorsWithValidationAnnotations(null, null);
        var testCommand2 = new TestCommandClassWithMultipleConstructorsWithValidationAnnotations(null);

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestCommandClassWithMultipleConstructorsWithValidationAnnotations.Payload1),
            nameof(TestCommandClassWithMultipleConstructorsWithValidationAnnotations.Payload2),
            nameof(TestCommandClassWithMultipleConstructorsWithValidationAnnotations.Payload3),
        }));

        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestCommandClassWithMultipleConstructorsWithValidationAnnotations.Payload1),
            nameof(TestCommandClassWithMultipleConstructorsWithValidationAnnotations.Payload2),
            nameof(TestCommandClassWithMultipleConstructorsWithValidationAnnotations.Payload3),
        }));
    }

    private sealed record TestCommandRecordWithMultipleConstructorsWithValidationAnnotations([Required] string? Payload1, [Required] string? Payload2)
    {
        public TestCommandRecordWithMultipleConstructorsWithValidationAnnotations(string? payload1, string? payload2, [Required] string? payload3)
            : this(payload1, payload2)
        {
            Payload3 = payload3;
        }

        [Required]
        public string? Payload3 { get; }
    }

    [Test]
    public void GivenValidCommandRecordWithMultipleConstructorsWithValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandRecordWithMultipleConstructorsWithValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithMultipleConstructorsWithValidationAnnotations>(host);

        var testCommand = new TestCommandRecordWithMultipleConstructorsWithValidationAnnotations("test", "test", "test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithMultipleConstructorsWithValidationAnnotations_ThrowsValidationExceptionWithErrorsForAllConstructorParameters()
    {
        using var host = CreateHost<TestCommandRecordWithMultipleConstructorsWithValidationAnnotations>();
        var handler = CreateHandler<TestCommandRecordWithMultipleConstructorsWithValidationAnnotations>(host);

        var testCommand = new TestCommandRecordWithMultipleConstructorsWithValidationAnnotations(null, null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestCommandRecordWithMultipleConstructorsWithValidationAnnotations.Payload1),
            nameof(TestCommandRecordWithMultipleConstructorsWithValidationAnnotations.Payload2),
            nameof(TestCommandRecordWithMultipleConstructorsWithValidationAnnotations.Payload3),
        }));
    }

    private sealed class TestCommandClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty([Required] string? param1, [Required] string? param2)
    {
        public string? Payload1 { get; } = param1;

        public string? Payload2 { get; } = param2;
    }

    [Test]
    public void GivenValidCommandClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>();
        var handler = CreateHandler<TestCommandClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>(host);

        var testCommand = new TestCommandClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty("test", "test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>();
        var handler = CreateHandler<TestCommandClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>(host);

        var testCommand = new TestCommandClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty(null, null);

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    private sealed record TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty
    {
        // ReSharper disable once ConvertToPrimaryConstructor
        public TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty([Required] string? param1, [Required] string? param2)
        {
            Payload1 = param1;
            Payload2 = param2;
        }

        public string? Payload1 { get; }

        public string? Payload2 { get; }
    }

    [Test]
    public void GivenValidCommandRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>();
        var handler = CreateHandler<TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>(host);

        var testCommand = new TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty("test", "test");

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>();
        var handler = CreateHandler<TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>(host);

        var testCommand = new TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty(null, null);

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    private sealed class TestCommandClassWithSingleComplexPropertyWithValidationAnnotation
    {
        [Required]
        public TestCommandClassWithSingleComplexPropertyWithValidationAnnotationProperty? OuterPayload { get; set; }
    }

    private sealed class TestCommandClassWithSingleComplexPropertyWithValidationAnnotationProperty
    {
        [Required]
        public string? InnerPayload { get; set; }
    }

    [Test]
    public void GivenValidCommandClassWithSingleComplexPropertyWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithSingleComplexPropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSingleComplexPropertyWithValidationAnnotation>(host);

        var testCommand = new TestCommandClassWithSingleComplexPropertyWithValidationAnnotation
        {
            OuterPayload = new() { InnerPayload = "test" },
        };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithSingleComplexPropertyWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandClassWithSingleComplexPropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSingleComplexPropertyWithValidationAnnotation>(host);

        var testCommand1 = new TestCommandClassWithSingleComplexPropertyWithValidationAnnotation();
        var testCommand2 = new TestCommandClassWithSingleComplexPropertyWithValidationAnnotation
        {
            OuterPayload = new() { InnerPayload = null },
        };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleComplexPropertyWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleComplexPropertyWithValidationAnnotation.OuterPayload) + "." + nameof(TestCommandClassWithSingleComplexPropertyWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed record TestRecordWithSingleComplexPropertyWithValidationAnnotation
    {
        [Required]
        public TestRecordWithSingleComplexPropertyWithValidationAnnotationProperty? OuterPayload { get; set; }
    }

    private sealed record TestRecordWithSingleComplexPropertyWithValidationAnnotationProperty
    {
        [Required]
        public string? InnerPayload { get; set; }
    }

    [Test]
    public void GivenValidCommandRecordWithSingleComplexPropertyWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSingleComplexPropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexPropertyWithValidationAnnotation>(host);

        var testCommand = new TestRecordWithSingleComplexPropertyWithValidationAnnotation
        {
            OuterPayload = new() { InnerPayload = "test" },
        };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithSingleComplexPropertyWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSingleComplexPropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexPropertyWithValidationAnnotation>(host);

        var testCommand1 = new TestRecordWithSingleComplexPropertyWithValidationAnnotation();
        var testCommand2 = new TestRecordWithSingleComplexPropertyWithValidationAnnotation
        {
            OuterPayload = new() { InnerPayload = null },
        };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestRecordWithSingleComplexPropertyWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestRecordWithSingleComplexPropertyWithValidationAnnotation.OuterPayload) + "." + nameof(TestRecordWithSingleComplexPropertyWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed class TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation([Required] TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotationProperty? outerPayload)
    {
        public TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotationProperty? OuterPayload { get; } = outerPayload;
    }

    private sealed class TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotationProperty([Required] string? innerPayload)
    {
        public string? InnerPayload { get; } = innerPayload;
    }

    [Test]
    public void GivenValidCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation>(host);

        var testCommand = new TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation(new("test"));

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation>(host);

        var testCommand1 = new TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation(null);
        var testCommand2 = new TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation(new(null));

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotation.OuterPayload) + "." + nameof(TestCommandClassWithSingleComplexConstructorParameterWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed record TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation([Required] TestRecordWithSingleComplexConstructorParameterWithValidationAnnotationProperty? OuterPayload);

    private sealed record TestRecordWithSingleComplexConstructorParameterWithValidationAnnotationProperty([Required] string? InnerPayload);

    [Test]
    public void GivenValidCommandRecordWithSingleComplexConstructorParameterWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation>(host);

        var testCommand = new TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation(new("test"));

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithSingleComplexConstructorParameterWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation>(host);

        var testCommand1 = new TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation(null);
        var testCommand2 = new TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation(new(null));

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation.OuterPayload) + "." + nameof(TestRecordWithSingleComplexConstructorParameterWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed class TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation
    {
        [Required]
        public List<TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotationProperty>? OuterPayload { get; set; }
    }

    private sealed class TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotationProperty
    {
        [Required]
        public string? InnerPayload { get; set; }
    }

    [Test]
    public void GivenValidCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation>(host);

        var testCommand = new TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation
        {
            OuterPayload = [new() { InnerPayload = "test" }],
        };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation>(host);

        var testCommand1 = new TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation();
        var testCommand2 = new TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation
        {
            OuterPayload = [new() { InnerPayload = null }],
        };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotation.OuterPayload) + "." + nameof(TestCommandClassWithSingleComplexEnumerablePropertyWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed record TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation
    {
        [Required]
        public List<TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotationProperty>? OuterPayload { get; set; }
    }

    private sealed record TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotationProperty
    {
        [Required]
        public string? InnerPayload { get; set; }
    }

    [Test]
    public void GivenValidCommandRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation>(host);

        var testCommand = new TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation
        {
            OuterPayload = [new() { InnerPayload = "test" }],
        };

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation>(host);

        var testCommand1 = new TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation();
        var testCommand2 = new TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation
        {
            OuterPayload = [new() { InnerPayload = null }],
        };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation.OuterPayload) + "." + nameof(TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed class TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([Required] List<TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty>? outerPayload)
    {
        public List<TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty>? OuterPayload { get; } = outerPayload;
    }

    private sealed class TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty([Required] string? innerPayload)
    {
        public string? InnerPayload { get; } = innerPayload;
    }

    [Test]
    public void GivenValidCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>(host);

        var testCommand = new TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([new("test")]);

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>(host);

        var testCommand1 = new TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation(null);
        var testCommand2 = new TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([new(null)]);

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation.OuterPayload) + "." + nameof(TestCommandClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed record TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([Required] List<TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty>? OuterPayload);

    private sealed record TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty([Required] string? InnerPayload);

    [Test]
    public void GivenValidCommandRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>(host);

        var testCommand = new TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([new("test")]);

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    [Test]
    public void GivenInvalidCommandRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>(host);

        var testCommand1 = new TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation(null);
        var testCommand2 = new TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([new(null)]);

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(testCommand2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EquivalentTo(new[] { nameof(TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation.OuterPayload) + "." + nameof(TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty.InnerPayload) }));
    }

    [Test]
    public void GivenHandlerWithRemovedValidationMiddleware_WhenCalledWithInvalidCommand_DoesNotThrowValidationException()
    {
        using var host = CreateHostWithAddedAndRemovedMiddleware<TestCommandClassWithSinglePropertyValidationAnnotation>();
        var handler = CreateHandler<TestCommandClassWithSinglePropertyValidationAnnotation>(host);

        var testCommand = new TestCommandClassWithSinglePropertyValidationAnnotation();

        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(testCommand));
    }

    private static IHost CreateHost<TCommand>()
        where TCommand : class
    {
        return new HostBuilder().ConfigureServices(
                                    services => services.AddConquerorCommandHandlerDelegate<TCommand, TestCommandResponse>(
                                                            (_, _, _) => Task.FromResult(new TestCommandResponse()),
                                                            pipeline => pipeline.UseDataAnnotationValidation()))
                                .Build();
    }

    private static IHost CreateHostWithAddedAndRemovedMiddleware<TCommand>()
        where TCommand : class
    {
        return new HostBuilder().ConfigureServices(
                                    services => services.AddConquerorCommandHandlerDelegate<TCommand, TestCommandResponse>(
                                        (_, _, _) => Task.FromResult(new TestCommandResponse()),
                                        pipeline => pipeline.UseDataAnnotationValidation().WithoutDataAnnotationValidation()))
                                .Build();
    }

    private static ICommandHandler<TCommand, TestCommandResponse> CreateHandler<TCommand>(IHost host)
        where TCommand : class
    {
        return host.Services.GetRequiredService<ICommandHandler<TCommand, TestCommandResponse>>();
    }

    private sealed record TestCommandResponse;
}
