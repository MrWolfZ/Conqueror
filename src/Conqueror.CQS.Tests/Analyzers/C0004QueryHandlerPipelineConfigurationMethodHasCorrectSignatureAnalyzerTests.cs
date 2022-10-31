using Conqueror.CQS.Analyzers;
using Conqueror.CQS.Tests.Analyzers.Verifiers;
using AnalyzerVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Conqueror.CQS.Analyzers.C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer>;
using CodeFixVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Conqueror.CQS.Analyzers.C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer,
    Conqueror.CQS.Analyzers.C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzerCodeFixProvider>;

namespace Conqueror.CQS.Tests.Analyzers
{
    public sealed class C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzerTests
    {
        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithoutMethod_NoDiagnosticsAreProduced()
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

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithoutMethod_NoDiagnosticsAreProduced()
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

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithCorrectSignature_NoDiagnosticsAreProduced()
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

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithCorrectSignature_NoDiagnosticsAreProduced()
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

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_DiagnosticsAreProduced()
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

                        public static string {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_CodeFixIsAppliedCorrectly()
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

                        public static string {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_DiagnosticsAreProduced()
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

                        public static string {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongReturnType_CodeFixIsAppliedCorrectly()
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

                        public static string {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongParameterType_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(string s)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_DiagnosticsAreProduced()
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

                        public void {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_CodeFixIsAppliedCorrectly()
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

                        public void {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_DiagnosticsAreProduced()
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

                        public void {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithNonStaticMethod_CodeFixIsAppliedCorrectly()
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

                        public void {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder, string s)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder, string s)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder, string s)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongNumberOfParameters_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder, string s)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}<T>(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}<T>(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_DiagnosticsAreProduced()
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

                        public static void {|#0:ConfigurePipeline|}<T>(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await AnalyzerVerifier.VerifyAnalyzerAsync(source, expected);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithTypeParameter_CodeFixIsAppliedCorrectly()
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

                        public static void {|#0:ConfigurePipeline|}<T>(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithInvalidConfigurationMethodWithArrowBody_CodeFixIsAppliedCorrectly()
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

                        public static string {|#0:ConfigurePipeline|}(IQueryPipelineBuilder builder) => string.Empty;

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder) => string.Empty;

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
                        {
                            await Task.Yield();
                            return new();
                        }
                    }
                }
            ".Dedent();

            var expected = AnalyzerVerifier.Diagnostic(C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId)
                                           .WithLocation(0);

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithInvalidMethodAndValidMethod_NoDiagnosticsAreProduced()
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

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder, string s)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithoutConfigurationInterfaceWithoutMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithoutConfigurationInterfaceWithMethodWithCorrectSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithoutConfigurationInterfaceWithMethodWithIncorrectSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static string ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
                    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public string ConfigurePipeline(IQueryPipelineBuilder builder)
                        {
                            return string.Empty;
                        }

                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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

                        public async Task ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
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
