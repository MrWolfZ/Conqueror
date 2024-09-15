namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares.Tests;

[TestFixture]
public sealed class DataAnnotationValidationCommandMiddlewareTests
{
    [Test]
    public async Task GivenHandlerWithValidationAnnotations_WhenExecutingWithInvalidCommand_ValidationExceptionIsThrown()
    {
        await using var serviceProvider = BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        Assert.ThrowsAsync<ValidationException>(() => handler.Handle(new(-1)));
    }

    [Test]
    public async Task GivenHandlerWithValidationAnnotations_WhenExecutingWithValidCommand_NoExceptionIsThrown()
    {
        await using var serviceProvider = BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        Assert.DoesNotThrowAsync(() => handler.Handle(new(1)));
    }

    private sealed record TestCommand(int Parameter)
    {
        [Range(1, int.MaxValue)]
        public int Parameter { get; } = Parameter;
    }

    private sealed record TestCommandResponse(int Value);

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public static void ConfigurePipeline(ICommandPipeline<TestCommand, TestCommandResponse> pipeline) =>
            pipeline.UseDataAnnotationValidation();

        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            // since we are only testing the input validation, the handler does not need to do anything
            return Task.FromResult<TestCommandResponse>(new(command.Parameter));
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                      .BuildServiceProvider();
    }
}
