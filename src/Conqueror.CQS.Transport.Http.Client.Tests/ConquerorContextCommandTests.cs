using System.Diagnostics;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [TestFixture]
    [NonParallelizable]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public class ConquerorContextCommandTests : TestBase
    {
        private static readonly Dictionary<string, string> ContextItems = new()
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "keyWith,Comma", "value" },
            { "key4", "valueWith,Comma" },
            { "keyWith=Equals", "value" },
            { "key6", "valueWith=Equals" },
        };

        [Test]
        public async Task GivenManuallyCreatedContextOnClientAndContextItemsInHandler_ItemsAreReturnedInClientContext()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

            var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ContextItems, context.Items);
        }

        [Test]
        public async Task GivenManuallyCreatedContextOnClientAndContextItemsInHandlerWithoutResponse_ItemsAreReturnedInClientContext()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

            var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ContextItems, context.Items);
        }

        [Test]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInHandler()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.AddOrReplaceItems(ContextItems);

            var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInHandlerAcrossMultipleInvocations()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.Items.Add(ContextItems.First());

            var observations = Resolve<TestObservations>();

            observations.ShouldAddItems = true;

            var allReceivedKeys = new List<string>();

            var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextItems.Count * 2 + 1));
        }

        [Test]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInHandlerWithoutResponse()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.AddOrReplaceItems(ContextItems);

            var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInHandlerWithoutResponseAcrossMultipleInvocations()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.Items.Add(ContextItems.First());

            var observations = Resolve<TestObservations>();

            observations.ShouldAddItems = true;

            var allReceivedKeys = new List<string>();

            var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextItems.Count * 2 + 1));
        }

        [Test]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInDifferentHandlersAcrossMultipleInvocations()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.Items.Add(ContextItems.First());

            var observations = Resolve<TestObservations>();

            observations.ShouldAddItems = true;

            var allReceivedKeys = new List<string>();

            var handler1 = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();
            var handler2 = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler2.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextItems.Count * 2 + 1));
        }

        [Test]
        public async Task GivenContextItemsInHandler_ContextIsReceivedInOuterHandler()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommand, OuterTestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = ResolveOnClient<TestObservations>().ReceivedOuterContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenContextItemsInHandlerWithoutResponse_ContextIsReceivedInOuterHandler()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = ResolveOnClient<TestObservations>().ReceivedOuterContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenContextItemsInOuterHandler_ContextIsReceivedInHandler()
        {
            ResolveOnClient<TestObservations>().ShouldAddOuterItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommand, OuterTestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenContextItemsInOuterHandler_ContextIsReceivedInHandlerWithoutResponse()
        {
            ResolveOnClient<TestObservations>().ShouldAddOuterItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenCommand_SameCommandIdIsObservedInTransportClientAndHandler()
        {
            var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedCommandIds, Resolve<TestObservations>().ReceivedCommandIds);
        }

        [Test]
        public async Task GivenCommandWithoutResponse_SameCommandIdIsObservedInTransportClientAndHandler()
        {
            var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedCommandIds, Resolve<TestObservations>().ReceivedCommandIds);
        }

        [Test]
        public async Task GivenCommandWithoutActiveClientSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
        {
            var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
        }

        [Test]
        public async Task GivenCommandWithoutResponseWithoutActiveClientSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
        {
            var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
        }

        [Test]
        public async Task GivenCommandWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler()
        {
            using var activity = StartActivity(nameof(GivenCommandWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler));

            var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
            Assert.AreEqual(activity.TraceId, Resolve<TestObservations>().ReceivedTraceIds.FirstOrDefault());
        }

        [Test]
        public async Task GivenCommandWithoutResponseWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler()
        {
            using var activity = StartActivity(nameof(GivenCommandWithoutResponseWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler));

            var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
            Assert.AreEqual(activity.TraceId, Resolve<TestObservations>().ReceivedTraceIds.FirstOrDefault());
        }

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQSHttpControllers();

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddSingleton<TestObservations>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

        protected override void ConfigureClientServices(IServiceCollection services)
        {
            _ = services.AddConquerorCQSHttpClientServices(o =>
            {
                o.HttpClientFactory = uri =>
                    throw new InvalidOperationException(
                        $"during tests all clients should be explicitly configured with the test http client; got request to create http client for base address '{uri}'");

                o.JsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                };
            });

            _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => new WrapperCommandTransportClient(b.UseHttp(HttpClient),
                                                                                                                                             b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                                                                                                                                             b.ServiceProvider.GetRequiredService<ICommandContextAccessor>(),
                                                                                                                                             b.ServiceProvider.GetRequiredService<TestObservations>()))
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(b => new WrapperCommandTransportClient(b.UseHttp(HttpClient),
                                                                                                                                       b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                                                                                                                                       b.ServiceProvider.GetRequiredService<ICommandContextAccessor>(),
                                                                                                                                       b.ServiceProvider.GetRequiredService<TestObservations>()));

            _ = services.AddTransient<OuterTestCommandHandler>()
                        .AddTransient<OuterTestCommandWithoutResponseHandler>()
                        .AddSingleton<TestObservations>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.Use(async (_, next) =>
            {
                // prevent leaking of client-side activity to server
                Activity.Current = null;
                await next();
            });

            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

        private static DisposableActivity StartActivity(string name)
        {
            var activitySource = new ActivitySource(name);

            var activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };

            ActivitySource.AddActivityListener(activityListener);

            var activity = activitySource.StartActivity()!;
            return new(activity.TraceId.ToString(), activitySource, activityListener, activity);
        }

        [HttpCommand]
        public sealed record TestCommand
        {
            public int Payload { get; init; }
        }

        public sealed record TestCommandResponse
        {
            public int Payload { get; init; }
        }

        [HttpCommand]
        public sealed record TestCommandWithoutResponse
        {
            public int Payload { get; init; }
        }

        public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandler(IConquerorContextAccessor conquerorContextAccessor,
                                      ICommandContextAccessor commandContextAccessor,
                                      TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedCommandIds.Add(commandContextAccessor.CommandContext?.CommandId);
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestCommandResponse());
            }
        }

        public sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandlerWithoutResponse(IConquerorContextAccessor conquerorContextAccessor,
                                                     ICommandContextAccessor commandContextAccessor,
                                                     TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
            }

            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedCommandIds.Add(commandContextAccessor.CommandContext?.CommandId);
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestCommandResponse());
            }
        }

        public sealed record OuterTestCommand;

        public sealed record OuterTestCommandResponse;

        public sealed class OuterTestCommandHandler : ICommandHandler<OuterTestCommand, OuterTestCommandResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly ICommandHandler<TestCommand, TestCommandResponse> nestedHandler;
            private readonly TestObservations testObservations;

            public OuterTestCommandHandler(IConquerorContextAccessor conquerorContextAccessor,
                                           ICommandContextAccessor commandContextAccessor,
                                           TestObservations testObservations,
                                           ICommandHandler<TestCommand, TestCommandResponse> nestedHandler)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
                this.nestedHandler = nestedHandler;
            }

            public async Task<OuterTestCommandResponse> ExecuteCommand(OuterTestCommand command, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedCommandIds.Add(commandContextAccessor.CommandContext?.CommandId);
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

                if (testObservations.ShouldAddOuterItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                _ = await nestedHandler.ExecuteCommand(new(), cancellationToken);
                testObservations.ReceivedOuterContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);
                return new();
            }
        }

        public sealed record OuterTestCommandWithoutResponse;

        public sealed class OuterTestCommandWithoutResponseHandler : ICommandHandler<OuterTestCommandWithoutResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly ICommandHandler<TestCommandWithoutResponse> nestedHandler;
            private readonly TestObservations testObservations;

            public OuterTestCommandWithoutResponseHandler(IConquerorContextAccessor conquerorContextAccessor,
                                                          ICommandContextAccessor commandContextAccessor,
                                                          TestObservations testObservations,
                                                          ICommandHandler<TestCommandWithoutResponse> nestedHandler)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
                this.nestedHandler = nestedHandler;
            }

            public async Task ExecuteCommand(OuterTestCommandWithoutResponse command, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedCommandIds.Add(commandContextAccessor.CommandContext?.CommandId);
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

                if (testObservations.ShouldAddOuterItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                await nestedHandler.ExecuteCommand(new(), cancellationToken);
                testObservations.ReceivedOuterContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);
            }
        }

        public sealed class TestObservations
        {
            public List<string?> ReceivedCommandIds { get; } = new();

            public List<string?> ReceivedTraceIds { get; } = new();

            public bool ShouldAddItems { get; set; }

            public bool ShouldAddOuterItems { get; set; }

            public IDictionary<string, string> ReceivedContextItems { get; } = new Dictionary<string, string>();

            public IDictionary<string, string> ReceivedOuterContextItems { get; } = new Dictionary<string, string>();
        }

        private sealed class WrapperCommandTransportClient : ICommandTransportClient
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;
            private readonly ICommandTransportClient wrapped;

            public WrapperCommandTransportClient(ICommandTransportClient wrapped,
                                                 IConquerorContextAccessor conquerorContextAccessor,
                                                 ICommandContextAccessor commandContextAccessor,
                                                 TestObservations testObservations)
            {
                this.wrapped = wrapped;
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
                where TCommand : class
            {
                testObservations.ReceivedCommandIds.Add(commandContextAccessor.CommandContext?.CommandId);
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

                return wrapped.ExecuteCommand<TCommand, TResponse>(command, cancellationToken);
            }
        }

        private sealed class DisposableActivity : IDisposable
        {
            private readonly IReadOnlyCollection<IDisposable> disposables;

            public DisposableActivity(string traceId, params IDisposable[] disposables)
            {
                TraceId = traceId;
                this.disposables = disposables;
            }

            public string TraceId { get; }

            public void Dispose()
            {
                foreach (var disposable in disposables.Reverse())
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
