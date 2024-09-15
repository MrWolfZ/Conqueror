using Conqueror.CQS.Analyzers;
using Conqueror.CQS.Tests.Analyzers.Verifiers;
using AnalyzerVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Conqueror.CQS.Analyzers.C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer>;
using CodeFixVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Conqueror.CQS.Analyzers.C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer,
    Conqueror.CQS.Analyzers.C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerCodeFixProvider>;

namespace Conqueror.CQS.Tests.Analyzers;

public sealed class C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerTests
{
    [Test]
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithoutMethod_DiagnosticsAreProduced()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = AnalyzerVerifier.Diagnostic(C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestQueryHandler");

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceWithoutMethod_DiagnosticsAreProduced()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse> {}
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler
                    {
                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = AnalyzerVerifier.Diagnostic(C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestQueryHandler");

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
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
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline) => throw new System.NotImplementedException();

                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = CodeFixVerifier.Diagnostic(C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                      .WithLocation(0)
                                      .WithArguments("TestQueryHandler");

        await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Test]
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse> {}
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
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
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse> {}
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline) => throw new System.NotImplementedException();

                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = CodeFixVerifier.Diagnostic(C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                      .WithLocation(0)
                                      .WithArguments("TestQueryHandler");

        await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Test]
    public async Task GivenQueryHandlerWithInterfaceWithoutAnyMembers_CodeFixIsAppliedCorrectly()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
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
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline) => throw new System.NotImplementedException();
                    }
                }
            ".Dedent();

        var expected = CodeFixVerifier.Diagnostic(C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                      .WithLocation(0)
                                      .WithArguments("TestQueryHandler");

        await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Test]
    public async Task GivenQueryHandlerWithInterfaceWithoutAnyMethods_CodeFixIsAppliedCorrectly()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

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
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline) => throw new System.NotImplementedException();
                    }
                }
            ".Dedent();

        var expected = CodeFixVerifier.Diagnostic(C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                      .WithLocation(0)
                                      .WithArguments("TestQueryHandler");

        await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Test]
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithMethod_NoDiagnosticsAreProduced()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline)
                        {
                        }
                    }
                }
            ".Dedent();

        await AnalyzerVerifier.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceWithMethod_NoDiagnosticsAreProduced()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse> {}
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler
                    {
                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline)
                        {
                        }
                    }
                }
            ".Dedent();

        await AnalyzerVerifier.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithMethodWithWrongSignature_NoDiagnosticsAreProduced()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public string ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline)
                        {
                            return string.Empty;
                        }
                    }
                }
            ".Dedent();

        await AnalyzerVerifier.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceWithMethodWrongSignature_NoDiagnosticsAreProduced()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse> {}
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler
                    {
                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public string ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline)
                        {
                            return string.Empty;
                        }
                    }
                }
            ".Dedent();

        await AnalyzerVerifier.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GivenTypeWithNonConquerorQueryHandlerInterface_NoDiagnosticsAreProduced()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public interface IQueryHandler<TQuery, TResponse> {}
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
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
    public async Task GivenSourceFromProjectWithoutConquerorReference_NoDiagnosticsAreProduced()
    {
        var source = @"
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public interface IQueryHandler<TQuery> {}
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery>
                    {
                        public async Task Handle(TestQuery query, CancellationToken cancellationToken = default)
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
