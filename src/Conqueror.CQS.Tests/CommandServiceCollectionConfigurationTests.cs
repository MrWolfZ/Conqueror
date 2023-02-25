namespace Conqueror.CQS.Tests
{
    [TestFixture]
    public sealed class CommandServiceCollectionConfigurationTests
    {
        [Test]
        public void GivenRegisteredHandlerType_AddingIdenticalHandlerDoesNotThrow()
        {
            var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>();

            Assert.DoesNotThrow(() => services.AddConquerorCommandHandler<TestCommandHandler>());
        }

        [Test]
        public void GivenRegisteredHandlerType_AddingIdenticalHandlerOnlyKeepsOneRegistration()
        {
            var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                                  .AddConquerorCommandHandler<TestCommandHandler>();

            Assert.AreEqual(1, services.Count(s => s.ServiceType == typeof(TestCommandHandler)));
            Assert.AreEqual(1, services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommand, TestCommandResponse>)));
        }

        [Test]
        public void GivenRegisteredHandlerTypeWithoutResponse_AddingIdenticalHandlerDoesNotThrow()
        {
            var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandler>();

            Assert.DoesNotThrow(() => services.AddConquerorCommandHandler<TestCommandWithoutResponseHandler>());
        }

        [Test]
        public void GivenRegisteredHandlerTypeWithoutResponse_AddingIdenticalHandlerOnlyKeepsOneRegistration()
        {
            var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                                                  .AddConquerorCommandHandler<TestCommandWithoutResponseHandler>();

            Assert.AreEqual(1, services.Count(s => s.ServiceType == typeof(TestCommandWithoutResponseHandler)));
            Assert.AreEqual(1, services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommandWithoutResponse>)));
        }

        [Test]
        public void GivenRegisteredHandlerType_AddingHandlerWithSameCommandAndResponseTypesThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandler<DuplicateTestCommandHandler>());
        }

        [Test]
        public void GivenRegisteredHandlerType_AddingHandlerWithSameCommandTypeAndDifferentResponseTypeThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandler<DuplicateTestCommandHandlerWithDifferentResponseType>());
        }

        [Test]
        public void GivenRegisteredHandlerType_AddingHandlerWithoutResponseWithSameCommandTypeThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandler<DuplicateTestCommandWithoutResponseHandler>());
        }

        [Test]
        public void GivenHandlerTypeWithInstanceFactory_AddedHandlerCanBeResolvedFromInterface()
        {
            var provider = new ServiceCollection().AddConquerorCommandHandler(_ => new TestCommandHandler())
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenMiddlewareTypeWithInstanceFactory_AddedMiddlewareCanBeUsedInPipeline()
        {
            var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandlerWithMiddleware>()
                                                  .AddConquerorCommandMiddleware(_ => new TestCommandMiddleware())
                                                  .BuildServiceProvider();

            Assert.DoesNotThrowAsync(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(new(), CancellationToken.None));
        }

        private sealed record TestCommand;

        private sealed record TestCommandWithoutResponse;

        private sealed record TestCommandResponse;

        private sealed record TestCommandResponse2;

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class DuplicateTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class DuplicateTestCommandHandlerWithDifferentResponseType : ICommandHandler<TestCommand, TestCommandResponse2>
        {
            public Task<TestCommandResponse2> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
        {
            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class DuplicateTestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
        {
            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class TestCommandHandlerWithMiddleware : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());

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
