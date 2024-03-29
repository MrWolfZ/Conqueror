using Conqueror.CQS.Analyzers;
using Conqueror.CQS.Tests.Analyzers.Verifiers;
using AnalyzerVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Conqueror.CQS.Analyzers.C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzer>;
using CodeFixVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Conqueror.CQS.Analyzers.C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzer,
    Conqueror.CQS.Analyzers.C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzerCodeFixProvider>;

namespace Conqueror.CQS.Tests.Analyzers;

public sealed class C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzerTests
{
    [Test]
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithoutConfigurationMethod_NoDiagnosticsAreProduced()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithoutConfigurationMethod_NoDiagnosticsAreProduced()
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
                    public sealed class TestQueryHandler : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWithCorrectSignature_NoDiagnosticsAreProduced()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWithCorrectSignature_NoDiagnosticsAreProduced()
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
                    public sealed class TestQueryHandler : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWithWrongSignature_NoDiagnosticsAreProduced()
    {
        var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static string ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithConfigurationMethodWrongSignature_NoDiagnosticsAreProduced()
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
                    public sealed class TestQueryHandler : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static string ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_DiagnosticsAreProduced()
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

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = AnalyzerVerifier.Diagnostic(C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestQueryHandler");

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_CodeFixIsAppliedCorrectly()
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

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, Conqueror.IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => throw new System.NotImplementedException();

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = AnalyzerVerifier.Diagnostic(C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestQueryHandler");

        await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Test]
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_DiagnosticsAreProduced()
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

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = AnalyzerVerifier.Diagnostic(C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestQueryHandler");

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_CodeFixIsAppliedCorrectly()
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

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, Conqueror.IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = AnalyzerVerifier.Diagnostic(C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestQueryHandler");

        await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Test]
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_DiagnosticsAreProduced()
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

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = AnalyzerVerifier.Diagnostic(C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestQueryHandler");

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceWithoutConfigurationInterfaceWithoutConfigurationMethod_CodeFixIsAppliedCorrectly()
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

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler, Conqueror.IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => throw new System.NotImplementedException();

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = AnalyzerVerifier.Diagnostic(C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestQueryHandler");

        await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Test]
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_DiagnosticsAreProduced()
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

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = AnalyzerVerifier.Diagnostic(C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestQueryHandler");

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceWithoutConfigurationInterfaceWithConfigurationMethod_CodeFixIsAppliedCorrectly()
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

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler, Conqueror.IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

        var expected = AnalyzerVerifier.Diagnostic(C0006QueryHandlerMustImplementPipelineConfigurationInterfaceAnalyzer.DiagnosticId)
                                       .WithLocation(0)
                                       .WithArguments("TestQueryHandler");

        await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Test]
    public async Task GivenCustomQueryHandlerInterface_NoDiagnosticsAreProduced()
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery>
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public string ConfigurePipeline()
                        {
                            return string.Empty;
                        }

                        public async Task ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
