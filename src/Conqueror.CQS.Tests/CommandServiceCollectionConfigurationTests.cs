namespace Conqueror.CQS.Tests
{
    [TestFixture]
    public sealed class CommandServiceCollectionConfigurationTests
    {
        [Test]
        public void GivenMultipleRegisteredIdenticalHandlerTypes_ConfiguringServiceCollectionDoesNotThrow()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandHandler>()
                                                  .AddTransient<TestCommandHandler>();

            Assert.DoesNotThrow(() => services.ConfigureConqueror());
        }

        [Test]
        public void GivenMultipleRegisteredHandlerTypesForSameCommandAndResponseTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandHandler>()
                                                  .AddTransient<DuplicateTestCommandHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }

        [Test]
        public void GivenMultipleRegisteredHandlerTypesForSameCommandAndDifferentResponseTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandHandler>()
                                                  .AddTransient<DuplicateTestCommandHandlerWithDifferentResponseType>();

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }

        [Test]
        public void GivenMultipleRegisteredHandlerTypesWithoutResponseForSameCommandType_ConfiguringServiceCollectionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandWithoutResponseHandler>()
                                                  .AddTransient<DuplicateTestCommandWithoutResponseHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }

        [Test]
        public void GivenHandlerTypeWithInstanceFactory_ConfiguringServiceCollectionRecognizesHandler()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient(_ => new TestCommandHandler())
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenMiddlewareTypeWithInstanceFactory_ConfiguringServiceCollectionRecognizesHandler()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandHandlerWithMiddleware>()
                                                  .AddTransient(_ => new TestCommandMiddleware())
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            Assert.DoesNotThrowAsync(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(new(), CancellationToken.None));
        }

        private sealed record TestCommand;

        private sealed record TestCommandWithoutResponse;

        private sealed record TestCommandResponse;

        private sealed record TestCommandResponse2;

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        private sealed class DuplicateTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        private sealed class DuplicateTestCommandHandlerWithDifferentResponseType : ICommandHandler<TestCommand, TestCommandResponse2>
        {
            public Task<TestCommandResponse2> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        private sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
        {
            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class DuplicateTestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
        {
            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class TestCommandHandlerWithMiddleware : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => Task.FromResult(new TestCommandResponse());

            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => pipeline.Use<TestCommandMiddleware>();
        }

        private sealed class TestCommandMiddleware : ICommandMiddleware
        {
            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }
    }
}
