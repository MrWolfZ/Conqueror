using Conqueror.CQS.Analyzers;
using VerifyCS = Conqueror.CQS.Tests.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Conqueror.CQS.Analyzers.ConquerorCqsConfigureCommandPipelineAnalyzer,
    Conqueror.CQS.Analyzers.ConquerorCqsConfigureCommandPipelineAnalyzerCodeFixProvider>;

namespace Conqueror.CQS.Tests.Analyzers
{
    public sealed class ConquerorCqsConfigureCommandPipelineAnalyzerTests
    {
        [Test]
        public async Task GivenEmptyCode_NoDiagnosticsAreProduced()
        {
            var test = string.Empty;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Test]
        public async Task GivenInvalidCode_DiagnosticsAreProduced()
        {
            var sourceCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                using System.Diagnostics;

                namespace ConsoleApplication1
                {
                    class {|#0:TypeName|}
                    {   
                    }
                }
            ";

            var fixedCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                using System.Diagnostics;

                namespace ConsoleApplication1
                {
                    class TYPENAME
                    {   
                    }
                }
            ";

            var expected = VerifyCS.Diagnostic(ConquerorCqsConfigureCommandPipelineAnalyzer.DiagnosticId).WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(sourceCode, expected, fixedCode);
        }
    }
}
