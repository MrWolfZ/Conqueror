using Conqueror.CQS.Analyzers;
using Conqueror.CQS.Tests.Analyzers.Verifiers;
using AnalyzerVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Conqueror.CQS.Analyzers.C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer>;
using CodeFixVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Conqueror.CQS.Analyzers.C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer,
    Conqueror.CQS.Analyzers.C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerCodeFixProvider>;

namespace Conqueror.CQS.Tests.Analyzers
{
    public sealed class C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerTests
    {
        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_DiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_DiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand>, IConfigureCommandPipeline
                    {
                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_DiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_DiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var fixedSource = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => throw new System.NotImplementedException();

                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = CodeFixVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                          .WithLocation(0)
                                          .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var fixedSource = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => throw new System.NotImplementedException();

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = CodeFixVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                          .WithLocation(0)
                                          .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var fixedSource = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => throw new System.NotImplementedException();

                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = CodeFixVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                          .WithLocation(0)
                                          .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var fixedSource = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => throw new System.NotImplementedException();

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = CodeFixVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                          .WithLocation(0)
                                          .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithInterfaceWithoutAnyMembers_CodeFixIsAppliedCorrectly()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                    }
                }
            ".Dedent();

            var fixedSource = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => throw new System.NotImplementedException();
                    }
                }
            ".Dedent();

            var expected = CodeFixVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                          .WithLocation(0)
                                          .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithInterfaceWithoutAnyMethods_CodeFixIsAppliedCorrectly()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }
                    }
                }
            ".Dedent();

            var fixedSource = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => throw new System.NotImplementedException();
                    }
                }
            ".Dedent();

            var expected = CodeFixVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                          .WithLocation(0)
                                          .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand>, IConfigureCommandPipeline
                    {
                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public string ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                            return string.Empty;
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand>, IConfigureCommandPipeline
                    {
                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }

                        public string ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                            return string.Empty;
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWrongSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public string ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                            return string.Empty;
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWrongSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }

                        public string ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                            return string.Empty;
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>
                    {
                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand>
                    {
                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>
                    {
                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand>
                    {
                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenTypeWithNonConquerorCommandHandlerInterface_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public interface ICommandHandler<TCommand, TResponse> {}
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenTypeWithNonConquerorCommandHandlerInterfaceWithoutResponse_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ICommandHandler<TCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand>, IConfigureCommandPipeline
                    {
                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenSourceFromProjectWithoutConquerorReference_NoDiagnosticsAreProduced()
        {
            var source = @"
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ICommandHandler<TCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand>
                    {
                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerWithoutConquerorReferenceAsync(source);
        }

        [Test]
        public async Task GivenEmptyCode_NoDiagnosticsAreProduced()
        {
            var source = string.Empty;

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }
    }
}
