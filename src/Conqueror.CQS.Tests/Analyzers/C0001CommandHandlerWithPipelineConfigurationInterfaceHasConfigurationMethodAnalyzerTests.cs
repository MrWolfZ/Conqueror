using Conqueror.CQS.Analyzers;
using Conqueror.CQS.Tests.Analyzers.Verifiers;
using AnalyzerVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Conqueror.CQS.Analyzers.C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer>;
using CodeFixVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Conqueror.CQS.Analyzers.C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer,
    Conqueror.CQS.Analyzers.C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerCodeFixProvider>;

namespace Conqueror.CQS.Tests.Analyzers;

public sealed class C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerTests
{
    [Test]
    public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithoutMethod_DiagnosticsAreProduced()
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

        var expected = AnalyzerVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestCommandHandler");

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutMethod_DiagnosticsAreProduced()
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

        var expected = AnalyzerVerifier.Diagnostic(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestCommandHandler");

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceWithoutMethod_DiagnosticsAreProduced()
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
    public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceWithoutMethod_DiagnosticsAreProduced()
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
    public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
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
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>
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
    public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
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
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand>
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
    public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
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
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler
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
    public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
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
                    public sealed class {|#0:TestCommandHandler|} : ITestCommandHandler
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
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>
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
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>
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
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>
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
                    public sealed class {|#0:TestCommandHandler|} : ICommandHandler<TestCommand, TestCommandResponse>
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
    public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithMethod_NoDiagnosticsAreProduced()
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
    public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithMethod_NoDiagnosticsAreProduced()
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
    public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceWithMethod_NoDiagnosticsAreProduced()
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
    public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceWithMethod_NoDiagnosticsAreProduced()
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
    public async Task GivenCommandHandlerWithPlainCommandHandlerInterfaceWithMethodWithWrongSignature_NoDiagnosticsAreProduced()
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
    public async Task GivenCommandHandlerWithoutResponseWithPlainCommandHandlerInterfaceWithMethodWithWrongSignature_NoDiagnosticsAreProduced()
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
    public async Task GivenCommandHandlerWithCustomCommandHandlerInterfaceWithMethodWrongSignature_NoDiagnosticsAreProduced()
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
    public async Task GivenCommandHandlerWithoutResponseWithCustomCommandHandlerInterfaceWithMethodWrongSignature_NoDiagnosticsAreProduced()
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
