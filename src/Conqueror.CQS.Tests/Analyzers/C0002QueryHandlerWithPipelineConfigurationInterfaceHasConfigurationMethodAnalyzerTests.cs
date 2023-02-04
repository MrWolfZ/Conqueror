using Conqueror.CQS.Analyzers;
using Conqueror.CQS.Tests.Analyzers.Verifiers;
using AnalyzerVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Conqueror.CQS.Analyzers.C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer>;
using CodeFixVerifier = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Conqueror.CQS.Analyzers.C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer,
    Conqueror.CQS.Analyzers.C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerCodeFixProvider>;

namespace Conqueror.CQS.Tests.Analyzers
{
    public sealed class C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerTests
    {
        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithoutMethod_DiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithoutMethod_DiagnosticsAreProduced()
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
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
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
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
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
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
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

            var expected = CodeFixVerifier.Diagnostic(C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                          .WithLocation(0)
                                          .WithArguments("TestQueryHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithoutMethod_CodeFixIsAppliedCorrectly()
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
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler, IConfigureQueryPipeline
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
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler, IConfigureQueryPipeline
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
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
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
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => throw new System.NotImplementedException();
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
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
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
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        private string field = string.Empty;

                        public TestQueryHandler() {}

                        public string Property { get; set; }

                        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => throw new System.NotImplementedException();
                    }
                }
            ".Dedent();

            var expected = CodeFixVerifier.Diagnostic(C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId)
                                          .WithLocation(0)
                                          .WithArguments("TestQueryHandler");

            await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethod_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
                        {
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethod_NoDiagnosticsAreProduced()
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
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
                        {
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWithWrongSignature_NoDiagnosticsAreProduced()
        {
            var source = @"
                using Conqueror;
                using System.Threading;
                using System.Threading.Tasks;

                namespace ConsoleApplication1
                {
                    public sealed record TestQuery;
                    public sealed record TestQueryResponse;
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public string ConfigurePipeline(IQueryPipelineBuilder pipeline)
                        {
                            return string.Empty;
                        }
                    }
                }
            ".Dedent();

            await AnalyzerVerifier.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task GivenQueryHandlerWithCustomQueryHandlerInterfaceAndConfigurationInterfaceWithMethodWrongSignature_NoDiagnosticsAreProduced()
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
                    public sealed class {|#0:TestQueryHandler|} : ITestQueryHandler, IConfigureQueryPipeline
                    {
                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public string ConfigurePipeline(IQueryPipelineBuilder pipeline)
                        {
                            return string.Empty;
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
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>
                    {
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
        public async Task GivenQueryHandlerWithPlainQueryHandlerInterfaceWithoutConfigurationInterfaceWithMethod_NoDiagnosticsAreProduced()
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
                        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
                        {
                            await Task.Yield();
                            return new();
                        }

                        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
                        {
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
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
                    {
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
                    public sealed class {|#0:TestQueryHandler|} : IQueryHandler<TestQuery>
                    {
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
}
