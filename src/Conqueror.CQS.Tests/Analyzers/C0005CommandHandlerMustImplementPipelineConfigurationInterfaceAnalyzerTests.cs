using Conqueror.CQS.Analyzers;
using Conqueror.CQS.Tests.Analyzers.Verifiers;
using AnalyzerVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Conqueror.CQS.Analyzers.C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer>;
using CodeFixVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Conqueror.CQS.Analyzers.C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer,
    Conqueror.CQS.Analyzers.C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzerCodeFixProvider>;

namespace Conqueror.CQS.Tests.Analyzers
{
    public sealed class C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzerTests
    {
        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithoutConfigurationMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
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

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithoutConfigurationMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>, IConfigureCommandPipeline
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
            
            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithoutConfigurationMethod_NoDiagnosticsAreProduced()
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
                    public sealed class TestCommandHandler : ITestCommandHandler, IConfigureCommandPipeline
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
            
            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithoutConfigurationMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class TestCommandHandler : ITestCommandHandler, IConfigureCommandPipeline
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
            
            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWithCorrectSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

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
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWithCorrectSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

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
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWithCorrectSignature_NoDiagnosticsAreProduced()
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
                    public sealed class TestCommandHandler : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

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
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWithCorrectSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class TestCommandHandler : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

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
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWithWrongSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static string ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

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
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWrongSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static string ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

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
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWrongSignature_NoDiagnosticsAreProduced()
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
                    public sealed class TestCommandHandler : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static string ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

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
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWrongSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class TestCommandHandler : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static string ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

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
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_DiagnosticsAreProduced()
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

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_CodeFixIsAppliedCorrectly()
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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, Conqueror.IConfigureCommandPipeline
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

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_DiagnosticsAreProduced()
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

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_CodeFixIsAppliedCorrectly()
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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>, Conqueror.IConfigureCommandPipeline
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

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_DiagnosticsAreProduced()
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
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_CodeFixIsAppliedCorrectly()
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
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, Conqueror.IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_DiagnosticsAreProduced()
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
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_CodeFixIsAppliedCorrectly()
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
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>, Conqueror.IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_DiagnosticsAreProduced()
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
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler
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

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_CodeFixIsAppliedCorrectly()
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
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler
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
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, Conqueror.IConfigureCommandPipeline
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

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_DiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler
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

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_CodeFixIsAppliedCorrectly()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler
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
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, Conqueror.IConfigureCommandPipeline
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

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_DiagnosticsAreProduced()
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
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_CodeFixIsAppliedCorrectly()
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
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

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
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, Conqueror.IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }
        
        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_DiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_CodeFixIsAppliedCorrectly()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public interface ITestCommandHandler : ICommandHandler<TestCommand> {}
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

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
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler, Conqueror.IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                           .WithLocation(0)
                                           .WithArguments("TestCommandHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }
        
        [Test]
        public async Task GivenCustomCommandHandlerInterface_NoDiagnosticsAreProduced()
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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>
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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public string ConfigurePipeline()
                        {
                            return string.Empty;
                        }

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
