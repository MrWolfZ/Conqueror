using Conqueror.CQS.Analyzers;
using Conqueror.CQS.Tests.Analyzers.Verifiers;
using AnalyzerVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Conqueror.CQS.Analyzers.C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer>;
using CodeFixVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Conqueror.CQS.Analyzers.C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer,
    Conqueror.CQS.Analyzers.C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzerCodeFixProvider>;

namespace Conqueror.CQS.Tests.Analyzers
{
    public sealed class C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzerTests
    {
        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_NoDiagnosticsAreProduced()
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
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_NoDiagnosticsAreProduced()
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
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_NoDiagnosticsAreProduced()
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
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithoutMethod_NoDiagnosticsAreProduced()
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
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithCorrectSignature_NoDiagnosticsAreProduced()
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
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithCorrectSignature_NoDiagnosticsAreProduced()
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
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithCorrectSignature_NoDiagnosticsAreProduced()
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
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithCorrectSignature_NoDiagnosticsAreProduced()
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
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_DiagnosticsAreProduced()
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

                        public static string {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_CodeFixIsAppliedCorrectly()
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

                        public static string {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var fixedSource = @"
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_DiagnosticsAreProduced()
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

                        public static string {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_CodeFixIsAppliedCorrectly()
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

                        public static string {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var fixedSource = @"
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
                            return string.Empty;
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_DiagnosticsAreProduced()
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

                        public static string {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_CodeFixIsAppliedCorrectly()
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

                        public static string {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var fixedSource = @"
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_DiagnosticsAreProduced()
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

                        public static string {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_CodeFixIsAppliedCorrectly()
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

                        public static string {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var fixedSource = @"
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
                            return string.Empty;
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
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
                    public sealed class TestCommandHandler : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
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
                    public sealed class TestCommandHandler : ITestCommandHandler, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_DiagnosticsAreProduced()
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

                        public void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_CodeFixIsAppliedCorrectly()
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

                        public void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_DiagnosticsAreProduced()
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

                        public void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_CodeFixIsAppliedCorrectly()
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

                        public void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_DiagnosticsAreProduced()
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

                        public void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_CodeFixIsAppliedCorrectly()
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

                        public void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_DiagnosticsAreProduced()
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

                        public void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_CodeFixIsAppliedCorrectly()
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

                        public void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder, string s)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder, string s)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder, string s)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder, string s)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder, string s)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder, string s)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder, string s)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder, string s)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}<T>(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}<T>(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}<T>(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}<T>(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}<T>(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}<T>(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}<T>(ICommandPipelineBuilder builder)
                        {
                        }

                        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}<T>(ICommandPipelineBuilder builder)
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

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithInvalidConfigurationMethodWIthArrowBody_CodeFixIsAppliedCorrectly()
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

                        public static string {|#0:ConfigurePipeline|}(ICommandPipelineBuilder builder) => string.Empty;

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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder) => string.Empty;

                        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenCommandHandlerWithInvalidMethodAndValidMethod_NoDiagnosticsAreProduced()
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

                        public static void ConfigurePipeline(ICommandPipelineBuilder builder, string s)
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
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithoutMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
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
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithMethodWithCorrectSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
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
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithMethodWithCorrectSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>
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
        public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithMethodWithIncorrectSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed record TestCommandResponse;
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
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
        public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutConfigurationInterfaceWithMethodWithIncorrectSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestCommand;
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>
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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public string ConfigurePipeline(ICommandPipelineBuilder builder)
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
                    public sealed class TestCommandHandler : ICommandHandler<TestCommand>, IConfigureCommandPipeline
                    {
                        private string field = string.Empty;

                        public TestCommandHandler() {}

                        public string Property { get; set; }

                        public string ConfigurePipeline(ICommandPipelineBuilder builder)
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
