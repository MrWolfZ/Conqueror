using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Hosting;

namespace Conqueror.CQS.Middleware.DataAnnotationValidation.Tests;

[TestFixture]
public sealed class DataAnnotationValidationQueryMiddlewareTests
{
    private sealed class TestQueryClassWithoutValidationAnnotations
    {
        public string? Payload { get; set; }
    }

    [Test]
    public void GivenQueryClassWithoutValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithoutValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithoutValidationAnnotations>(host);

        var testQuery = new TestQueryClassWithoutValidationAnnotations { Payload = "test" };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    private sealed record TestQueryRecordWithoutValidationAnnotations(string? Payload);

    [Test]
    public void GivenQueryRecordWithoutValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryRecordWithoutValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithoutValidationAnnotations>(host);

        var testQuery = new TestQueryRecordWithoutValidationAnnotations("test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    private sealed class TestQueryClassWithoutPayload;

    [Test]
    public void GivenQueryClassWithoutPayload_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithoutPayload>();
        var handler = CreateHandler<TestQueryClassWithoutPayload>(host);

        var testQuery = new TestQueryClassWithoutPayload();

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    private sealed record TestRecordWithoutPayload;

    [Test]
    public void GivenQueryRecordWithoutPayload_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithoutPayload>();
        var handler = CreateHandler<TestRecordWithoutPayload>(host);

        var testQuery = new TestRecordWithoutPayload();

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    private sealed class TestQueryClassWithSinglePropertyValidationAnnotation
    {
        [Required]
        public string? Payload { get; set; }
    }

    [Test]
    public void GivenValidQueryClassWithSinglePropertyValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithSinglePropertyValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSinglePropertyValidationAnnotation>(host);

        var testQuery = new TestQueryClassWithSinglePropertyValidationAnnotation { Payload = "test" };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithSinglePropertyValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryClassWithSinglePropertyValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSinglePropertyValidationAnnotation>(host);

        var testQuery = new TestQueryClassWithSinglePropertyValidationAnnotation();

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSinglePropertyValidationAnnotation.Payload) }));
    }

    private sealed record TestRecordWithSinglePropertyValidationAnnotation
    {
        [Required]
        public string? Payload { get; set; }
    }

    [Test]
    public void GivenValidQueryRecordWithSinglePropertyValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSinglePropertyValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSinglePropertyValidationAnnotation>(host);

        var testQuery = new TestRecordWithSinglePropertyValidationAnnotation { Payload = "test" };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithSinglePropertyValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSinglePropertyValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSinglePropertyValidationAnnotation>(host);

        var testQuery = new TestRecordWithSinglePropertyValidationAnnotation();

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestRecordWithSinglePropertyValidationAnnotation.Payload) }));
    }

    private sealed record TestRecordWithSinglePropertyValidationAnnotationInConstructor([property: Required] string? Payload);

    [Test]
    public void GivenValidQueryRecordWithSinglePropertyValidationAnnotationInConstructor_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSinglePropertyValidationAnnotationInConstructor>();
        var handler = CreateHandler<TestRecordWithSinglePropertyValidationAnnotationInConstructor>(host);

        var testQuery = new TestRecordWithSinglePropertyValidationAnnotationInConstructor("test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithSinglePropertyValidationAnnotationInConstructor_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSinglePropertyValidationAnnotationInConstructor>();
        var handler = CreateHandler<TestRecordWithSinglePropertyValidationAnnotationInConstructor>(host);

        var testQuery = new TestRecordWithSinglePropertyValidationAnnotationInConstructor(null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestRecordWithSinglePropertyValidationAnnotationInConstructor.Payload) }));
    }

    private sealed class TestQueryClassWithSinglePropertyWithMultipleValidationAnnotations
    {
        [MinLength(2)]
        [MaxLength(5)]
        public string? Payload { get; set; }
    }

    [Test]
    public void GivenValidQueryClassWithSinglePropertyWithMultipleValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithSinglePropertyWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithSinglePropertyWithMultipleValidationAnnotations>(host);

        var testQuery = new TestQueryClassWithSinglePropertyWithMultipleValidationAnnotations { Payload = "test" };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithSinglePropertyWithMultipleValidationAnnotations_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryClassWithSinglePropertyWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithSinglePropertyWithMultipleValidationAnnotations>(host);

        var testQuery1 = new TestQueryClassWithSinglePropertyWithMultipleValidationAnnotations { Payload = "t" };
        var testQuery2 = new TestQueryClassWithSinglePropertyWithMultipleValidationAnnotations { Payload = "too-long" };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSinglePropertyWithMultipleValidationAnnotations.Payload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSinglePropertyWithMultipleValidationAnnotations.Payload) }));
    }

    private sealed record TestQueryRecordWithSinglePropertyWithMultipleValidationAnnotations
    {
        [MinLength(2)]
        [MaxLength(5)]
        public string? Payload { get; set; }
    }

    [Test]
    public void GivenValidQueryRecordWithSinglePropertyWithMultipleValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryRecordWithSinglePropertyWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithSinglePropertyWithMultipleValidationAnnotations>(host);

        var testQuery = new TestQueryRecordWithSinglePropertyWithMultipleValidationAnnotations { Payload = "test" };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithSinglePropertyWithMultipleValidationAnnotations_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryRecordWithSinglePropertyWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithSinglePropertyWithMultipleValidationAnnotations>(host);

        var testQuery1 = new TestQueryRecordWithSinglePropertyWithMultipleValidationAnnotations { Payload = "t" };
        var testQuery2 = new TestQueryRecordWithSinglePropertyWithMultipleValidationAnnotations { Payload = "too-long" };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryRecordWithSinglePropertyWithMultipleValidationAnnotations.Payload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryRecordWithSinglePropertyWithMultipleValidationAnnotations.Payload) }));
    }

    private sealed class TestQueryClassWithSingleConstructorValidationAnnotation([Required] string? payload)
    {
        public string? Payload { get; } = payload;
    }

    [Test]
    public void GivenValidQueryClassWithSingleConstructorValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithSingleConstructorValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSingleConstructorValidationAnnotation>(host);

        var testQuery = new TestQueryClassWithSingleConstructorValidationAnnotation("test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithSingleConstructorValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryClassWithSingleConstructorValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSingleConstructorValidationAnnotation>(host);

        var testQuery = new TestQueryClassWithSingleConstructorValidationAnnotation(null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleConstructorValidationAnnotation.Payload) }));
    }

    private sealed record TestQueryRecordWithSingleConstructorValidationAnnotation([Required] string? Payload);

    [Test]
    public void GivenValidQueryRecordWithSingleConstructorValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryRecordWithSingleConstructorValidationAnnotation>();
        var handler = CreateHandler<TestQueryRecordWithSingleConstructorValidationAnnotation>(host);

        var testQuery = new TestQueryRecordWithSingleConstructorValidationAnnotation("test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithSingleConstructorValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryRecordWithSingleConstructorValidationAnnotation>();
        var handler = CreateHandler<TestQueryRecordWithSingleConstructorValidationAnnotation>(host);

        var testQuery = new TestQueryRecordWithSingleConstructorValidationAnnotation(null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryRecordWithSingleConstructorValidationAnnotation.Payload) }));
    }

    private sealed class TestQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations([MinLength(2)] [MaxLength(5)] string? payload)
    {
        public string? Payload { get; } = payload;
    }

    [Test]
    public void GivenValidQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations>(host);

        var testQuery = new TestQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations("test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations>(host);

        var testQuery1 = new TestQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations("t");
        var testQuery2 = new TestQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations("too-long");

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations.Payload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleConstructorParameterWithMultipleValidationAnnotations.Payload) }));
    }

    private sealed record TestQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations([MinLength(2)] [MaxLength(5)] string? Payload);

    [Test]
    public void GivenValidQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations>(host);

        var testQuery = new TestQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations("test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations>(host);

        var testQuery1 = new TestQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations("t");
        var testQuery2 = new TestQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations("too-long");

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations.Payload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryRecordWithSingleConstructorParameterWithMultipleValidationAnnotations.Payload) }));
    }

    private sealed class TestQueryClassWithMultiplePropertyValidationAnnotations
    {
        [Required]
        public string? Payload1 { get; set; }

        [Required]
        public string? Payload2 { get; set; }
    }

    [Test]
    public void GivenValidQueryClassWithMultiplePropertyValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithMultiplePropertyValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithMultiplePropertyValidationAnnotations>(host);

        var testQuery = new TestQueryClassWithMultiplePropertyValidationAnnotations
        {
            Payload1 = "test",
            Payload2 = "test",
        };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithMultiplePropertyValidationAnnotations_ThrowsValidationExceptionWithMultipleErrors()
    {
        using var host = CreateHost<TestQueryClassWithMultiplePropertyValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithMultiplePropertyValidationAnnotations>(host);

        var testQuery = new TestQueryClassWithMultiplePropertyValidationAnnotations
        {
            Payload1 = null,
            Payload2 = null,
        };

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EqualTo(new[]
        {
            nameof(TestQueryClassWithMultiplePropertyValidationAnnotations.Payload1),
            nameof(TestQueryClassWithMultiplePropertyValidationAnnotations.Payload2),
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
    public void GivenValidQueryRecordWithMultiplePropertyValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithMultiplePropertyValidationAnnotations>();
        var handler = CreateHandler<TestRecordWithMultiplePropertyValidationAnnotations>(host);

        var testQuery = new TestRecordWithMultiplePropertyValidationAnnotations
        {
            Payload1 = "test",
            Payload2 = "test",
        };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithMultiplePropertyValidationAnnotations_ThrowsValidationExceptionWithMultipleErrors()
    {
        using var host = CreateHost<TestRecordWithMultiplePropertyValidationAnnotations>();
        var handler = CreateHandler<TestRecordWithMultiplePropertyValidationAnnotations>(host);

        var testQuery = new TestRecordWithMultiplePropertyValidationAnnotations
        {
            Payload1 = null,
            Payload2 = null,
        };

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EqualTo(new[]
        {
            nameof(TestRecordWithMultiplePropertyValidationAnnotations.Payload1),
            nameof(TestRecordWithMultiplePropertyValidationAnnotations.Payload2),
        }));
    }

    private sealed class TestQueryClassWithMultipleConstructorValidationAnnotations([Required] string? payload1, [Required] string? payload2)
    {
        public string? Payload1 { get; } = payload1;

        public string? Payload2 { get; } = payload2;
    }

    [Test]
    public void GivenValidQueryClassWithMultipleConstructorValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithMultipleConstructorValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithMultipleConstructorValidationAnnotations>(host);

        var testQuery = new TestQueryClassWithMultipleConstructorValidationAnnotations("test", "test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithMultipleConstructorValidationAnnotations_ThrowsValidationExceptionWithMultipleErrors()
    {
        using var host = CreateHost<TestQueryClassWithMultipleConstructorValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithMultipleConstructorValidationAnnotations>(host);

        var testQuery = new TestQueryClassWithMultipleConstructorValidationAnnotations(null, null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EqualTo(new[]
        {
            nameof(TestQueryClassWithMultipleConstructorValidationAnnotations.Payload1),
            nameof(TestQueryClassWithMultipleConstructorValidationAnnotations.Payload2),
        }));
    }

    private sealed record TestQueryRecordWithMultipleConstructorValidationAnnotations([Required] string? Payload1, [Required] string? Payload2);

    [Test]
    public void GivenValidQueryRecordWithMultipleConstructorValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryRecordWithMultipleConstructorValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithMultipleConstructorValidationAnnotations>(host);

        var testQuery = new TestQueryRecordWithMultipleConstructorValidationAnnotations("test", "test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithMultipleConstructorValidationAnnotations_ThrowsValidationExceptionWithMultipleErrors()
    {
        using var host = CreateHost<TestQueryRecordWithMultipleConstructorValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithMultipleConstructorValidationAnnotations>(host);

        var testQuery = new TestQueryRecordWithMultipleConstructorValidationAnnotations(null, null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EqualTo(new[]
        {
            nameof(TestQueryRecordWithMultipleConstructorValidationAnnotations.Payload1),
            nameof(TestQueryRecordWithMultipleConstructorValidationAnnotations.Payload2),
        }));
    }

    private sealed class TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations([Required] string? payload1, [Required] string? payload2)
    {
        public string? Payload1 { get; } = payload1;

        public string? Payload2 { get; } = payload2;

        [Required]
        public string? Payload3 { get; set; }

        [Required]
        public string? Payload4 { get; set; }
    }

    [Test]
    public void GivenValidQueryClassWithMultiplePropertyAndConstructorValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations>(host);

        var testQuery = new TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations("test", "test")
        {
            Payload3 = "test",
            Payload4 = "test",
        };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithMultiplePropertyAndConstructorValidationAnnotations_ThrowsValidationExceptionWithErrorsForAllPropertiesAndConstructorParameters()
    {
        using var host = CreateHost<TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations>(host);

        var testQuery = new TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations(null, null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations.Payload1),
            nameof(TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations.Payload2),
            nameof(TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations.Payload3),
            nameof(TestQueryClassWithMultiplePropertyAndConstructorValidationAnnotations.Payload4),
        }));
    }

    private sealed record TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations([Required] string? Payload1, [Required] string? Payload2, [property: Required] string? Payload3, string? Payload4)
    {
        [Required]
        public string? Payload4 { get; } = Payload4;
    }

    [Test]
    public void GivenValidQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations>(host);

        var testQuery = new TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations("test", "test", "test", "test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations_ThrowsValidationExceptionWithErrorsForAllPropertiesAndConstructorParameters()
    {
        using var host = CreateHost<TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations>(host);

        var testQuery = new TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations(null, null, null, null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations.Payload1),
            nameof(TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations.Payload2),
            nameof(TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations.Payload3),
            nameof(TestQueryRecordWithMultiplePropertyAndConstructorValidationAnnotations.Payload4),
        }));
    }

    private sealed class TestQueryClassWithMultipleConstructorsWithValidationAnnotations
    {
        public TestQueryClassWithMultipleConstructorsWithValidationAnnotations([Required] string? payload1, [Required] string? payload2)
        {
            Payload1 = payload1;
            Payload2 = payload2;
        }

        public TestQueryClassWithMultipleConstructorsWithValidationAnnotations([Required] string? payload3)
        {
            Payload3 = payload3;
        }

        public string? Payload1 { get; }

        public string? Payload2 { get; }

        public string? Payload3 { get; set; }
    }

    [Test]
    public void GivenValidQueryClassWithMultipleConstructorsWithValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithMultipleConstructorsWithValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithMultipleConstructorsWithValidationAnnotations>(host);

        var testQuery = new TestQueryClassWithMultipleConstructorsWithValidationAnnotations("test", "test")
        {
            Payload3 = "test",
        };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithMultipleConstructorsWithValidationAnnotations_ThrowsValidationExceptionWithErrorsForAllConstructorParameters()
    {
        using var host = CreateHost<TestQueryClassWithMultipleConstructorsWithValidationAnnotations>();
        var handler = CreateHandler<TestQueryClassWithMultipleConstructorsWithValidationAnnotations>(host);

        var testQuery1 = new TestQueryClassWithMultipleConstructorsWithValidationAnnotations(null, null);
        var testQuery2 = new TestQueryClassWithMultipleConstructorsWithValidationAnnotations(null);

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[]
        {
            nameof(TestQueryClassWithMultipleConstructorsWithValidationAnnotations.Payload1),
            nameof(TestQueryClassWithMultipleConstructorsWithValidationAnnotations.Payload2),
            nameof(TestQueryClassWithMultipleConstructorsWithValidationAnnotations.Payload3),
        }));

        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[]
        {
            nameof(TestQueryClassWithMultipleConstructorsWithValidationAnnotations.Payload1),
            nameof(TestQueryClassWithMultipleConstructorsWithValidationAnnotations.Payload2),
            nameof(TestQueryClassWithMultipleConstructorsWithValidationAnnotations.Payload3),
        }));
    }

    private sealed record TestQueryRecordWithMultipleConstructorsWithValidationAnnotations([Required] string? Payload1, [Required] string? Payload2)
    {
        public TestQueryRecordWithMultipleConstructorsWithValidationAnnotations(string? payload1, string? payload2, [Required] string? payload3)
            : this(payload1, payload2)
        {
            Payload3 = payload3;
        }

        [Required]
        public string? Payload3 { get; }
    }

    [Test]
    public void GivenValidQueryRecordWithMultipleConstructorsWithValidationAnnotations_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryRecordWithMultipleConstructorsWithValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithMultipleConstructorsWithValidationAnnotations>(host);

        var testQuery = new TestQueryRecordWithMultipleConstructorsWithValidationAnnotations("test", "test", "test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithMultipleConstructorsWithValidationAnnotations_ThrowsValidationExceptionWithErrorsForAllConstructorParameters()
    {
        using var host = CreateHost<TestQueryRecordWithMultipleConstructorsWithValidationAnnotations>();
        var handler = CreateHandler<TestQueryRecordWithMultipleConstructorsWithValidationAnnotations>(host);

        var testQuery = new TestQueryRecordWithMultipleConstructorsWithValidationAnnotations(null, null);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery));

        Assert.That(exception?.ValidationResult.MemberNames, Is.EquivalentTo(new[]
        {
            nameof(TestQueryRecordWithMultipleConstructorsWithValidationAnnotations.Payload1),
            nameof(TestQueryRecordWithMultipleConstructorsWithValidationAnnotations.Payload2),
            nameof(TestQueryRecordWithMultipleConstructorsWithValidationAnnotations.Payload3),
        }));
    }

    private sealed class TestQueryClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty([Required] string? param1, [Required] string? param2)
    {
        public string? Payload1 { get; } = param1;

        public string? Payload2 { get; } = param2;
    }

    [Test]
    public void GivenValidQueryClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>();
        var handler = CreateHandler<TestQueryClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>(host);

        var testQuery = new TestQueryClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty("test", "test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>();
        var handler = CreateHandler<TestQueryClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>(host);

        var testQuery = new TestQueryClassWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty(null, null);

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
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
    public void GivenValidQueryRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>();
        var handler = CreateHandler<TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>(host);

        var testQuery = new TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty("test", "test");

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>();
        var handler = CreateHandler<TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty>(host);

        var testQuery = new TestRecordWithMultipleConstructorValidationAnnotationsWithoutMatchingProperty(null, null);

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    private sealed class TestQueryClassWithSingleComplexPropertyWithValidationAnnotation
    {
        [Required]
        public TestQueryClassWithSingleComplexPropertyWithValidationAnnotationProperty? OuterPayload { get; set; }
    }

    private sealed class TestQueryClassWithSingleComplexPropertyWithValidationAnnotationProperty
    {
        [Required]
        public string? InnerPayload { get; set; }
    }

    [Test]
    public void GivenValidQueryClassWithSingleComplexPropertyWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithSingleComplexPropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSingleComplexPropertyWithValidationAnnotation>(host);

        var testQuery = new TestQueryClassWithSingleComplexPropertyWithValidationAnnotation
        {
            OuterPayload = new() { InnerPayload = "test" },
        };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithSingleComplexPropertyWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryClassWithSingleComplexPropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSingleComplexPropertyWithValidationAnnotation>(host);

        var testQuery1 = new TestQueryClassWithSingleComplexPropertyWithValidationAnnotation();
        var testQuery2 = new TestQueryClassWithSingleComplexPropertyWithValidationAnnotation
        {
            OuterPayload = new() { InnerPayload = null },
        };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleComplexPropertyWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleComplexPropertyWithValidationAnnotation.OuterPayload) + "." + nameof(TestQueryClassWithSingleComplexPropertyWithValidationAnnotationProperty.InnerPayload) }));
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
    public void GivenValidQueryRecordWithSingleComplexPropertyWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSingleComplexPropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexPropertyWithValidationAnnotation>(host);

        var testQuery = new TestRecordWithSingleComplexPropertyWithValidationAnnotation
        {
            OuterPayload = new() { InnerPayload = "test" },
        };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithSingleComplexPropertyWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSingleComplexPropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexPropertyWithValidationAnnotation>(host);

        var testQuery1 = new TestRecordWithSingleComplexPropertyWithValidationAnnotation();
        var testQuery2 = new TestRecordWithSingleComplexPropertyWithValidationAnnotation
        {
            OuterPayload = new() { InnerPayload = null },
        };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestRecordWithSingleComplexPropertyWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestRecordWithSingleComplexPropertyWithValidationAnnotation.OuterPayload) + "." + nameof(TestRecordWithSingleComplexPropertyWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed class TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation([Required] TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotationProperty? outerPayload)
    {
        public TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotationProperty? OuterPayload { get; } = outerPayload;
    }

    private sealed class TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotationProperty([Required] string? innerPayload)
    {
        public string? InnerPayload { get; } = innerPayload;
    }

    [Test]
    public void GivenValidQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation>(host);

        var testQuery = new TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation(new("test"));

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation>(host);

        var testQuery1 = new TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation(null);
        var testQuery2 = new TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation(new(null));

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotation.OuterPayload) + "." + nameof(TestQueryClassWithSingleComplexConstructorParameterWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed record TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation([Required] TestRecordWithSingleComplexConstructorParameterWithValidationAnnotationProperty? OuterPayload);

    private sealed record TestRecordWithSingleComplexConstructorParameterWithValidationAnnotationProperty([Required] string? InnerPayload);

    [Test]
    public void GivenValidQueryRecordWithSingleComplexConstructorParameterWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation>(host);

        var testQuery = new TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation(new("test"));

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithSingleComplexConstructorParameterWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation>(host);

        var testQuery1 = new TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation(null);
        var testQuery2 = new TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation(new(null));

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestRecordWithSingleComplexConstructorParameterWithValidationAnnotation.OuterPayload) + "." + nameof(TestRecordWithSingleComplexConstructorParameterWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed class TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation
    {
        [Required]
        public List<TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotationProperty>? OuterPayload { get; set; }
    }

    private sealed class TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotationProperty
    {
        [Required]
        public string? InnerPayload { get; set; }
    }

    [Test]
    public void GivenValidQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation>(host);

        var testQuery = new TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation
        {
            OuterPayload = [new() { InnerPayload = "test" }],
        };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation>(host);

        var testQuery1 = new TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation();
        var testQuery2 = new TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation
        {
            OuterPayload = [new() { InnerPayload = null }],
        };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotation.OuterPayload) + "." + nameof(TestQueryClassWithSingleComplexEnumerablePropertyWithValidationAnnotationProperty.InnerPayload) }));
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
    public void GivenValidQueryRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation>(host);

        var testQuery = new TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation
        {
            OuterPayload = [new() { InnerPayload = "test" }],
        };

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation>(host);

        var testQuery1 = new TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation();
        var testQuery2 = new TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation
        {
            OuterPayload = [new() { InnerPayload = null }],
        };

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotation.OuterPayload) + "." + nameof(TestRecordWithSingleComplexEnumerablePropertyWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed class TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([Required] List<TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty>? outerPayload)
    {
        public List<TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty>? OuterPayload { get; } = outerPayload;
    }

    private sealed class TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty([Required] string? innerPayload)
    {
        public string? InnerPayload { get; } = innerPayload;
    }

    [Test]
    public void GivenValidQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>(host);

        var testQuery = new TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([new("test")]);

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>(host);

        var testQuery1 = new TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation(null);
        var testQuery2 = new TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([new(null)]);

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation.OuterPayload) + "." + nameof(TestQueryClassWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty.InnerPayload) }));
    }

    private sealed record TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([Required] List<TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty>? OuterPayload);

    private sealed record TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty([Required] string? InnerPayload);

    [Test]
    public void GivenValidQueryRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation_DoesNotThrowValidationException()
    {
        using var host = CreateHost<TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>(host);

        var testQuery = new TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([new("test")]);

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    [Test]
    public void GivenInvalidQueryRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation_ThrowsValidationExceptionWithSingleError()
    {
        using var host = CreateHost<TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>();
        var handler = CreateHandler<TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation>(host);

        var testQuery1 = new TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation(null);
        var testQuery2 = new TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation([new(null)]);

        var exception1 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery1));
        var exception2 = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(testQuery2));

        Assert.That(exception1?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation.OuterPayload) }));
        Assert.That(exception2?.ValidationResult.MemberNames, Is.EqualTo(new[] { nameof(TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotation.OuterPayload) + "." + nameof(TestRecordWithSingleComplexEnumerableConstructorParameterWithValidationAnnotationProperty.InnerPayload) }));
    }

    [Test]
    public void GivenHandlerWithRemovedValidationMiddleware_WhenCalledWithInvalidQuery_DoesNotThrowValidationException()
    {
        using var host = CreateHostWithAddedAndRemovedMiddleware<TestQueryClassWithSinglePropertyValidationAnnotation>();
        var handler = CreateHandler<TestQueryClassWithSinglePropertyValidationAnnotation>(host);

        var testQuery = new TestQueryClassWithSinglePropertyValidationAnnotation();

        Assert.DoesNotThrowAsync(() => handler.Handle(testQuery));
    }

    private static IHost CreateHost<TQuery>()
        where TQuery : class
    {
        return new HostBuilder().ConfigureServices(
                                    services => services.AddConquerorQueryHandlerDelegate<TQuery, TestQueryResponse>(
                                        (_, _, _) => Task.FromResult(new TestQueryResponse()),
                                        pipeline => pipeline.UseDataAnnotationValidation()))
                                .Build();
    }

    private static IHost CreateHostWithAddedAndRemovedMiddleware<TQuery>()
        where TQuery : class
    {
        return new HostBuilder().ConfigureServices(
                                    services => services.AddConquerorQueryHandlerDelegate<TQuery, TestQueryResponse>(
                                        (_, _, _) => Task.FromResult(new TestQueryResponse()),
                                        pipeline => pipeline.UseDataAnnotationValidation().WithoutDataAnnotationValidation()))
                                .Build();
    }

    private static IQueryHandler<TQuery, TestQueryResponse> CreateHandler<TQuery>(IHost host)
        where TQuery : class
    {
        return host.Services.GetRequiredService<IQueryHandler<TQuery, TestQueryResponse>>();
    }

    private sealed record TestQueryResponse;
}
