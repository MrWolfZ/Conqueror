using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Conqueror.SourceGenerators.Tests;

internal static class TestHelpers
{
    public static void ScrubExpectedChanges(this VerifySettings settings)
    {
        settings.ScrubLinesWithReplace(
            line => line.Replace(
                $"""GeneratedCodeAttribute("Conqueror.SourceGenerators", "{Constants.Version}")""",
                """GeneratedCodeAttribute("Conqueror.SourceGenerators", "FIXED_VERSION")"""));
    }

    public static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput(IIncrementalGenerator[] generators, Options opts)
    {
        var (diagnostics, trees) = GetGeneratedTrees(generators, opts, opts.Stages ?? GetTrackingNames<TrackingNames>());

        // exclude generated static attribute files from the output
        var output = trees.Select(t => t.ToString()).LastOrDefault() ?? string.Empty;
        return (diagnostics, output);
    }

    public static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<SyntaxTree> Output) GetGeneratedTrees<TTrackingNames>(
        IIncrementalGenerator[] generators, Options options)
    {
        return GetGeneratedTrees(generators, options, options.Stages ?? GetTrackingNames<TTrackingNames>());
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<SyntaxTree> SyntaxTrees) GetGeneratedTrees(
        IIncrementalGenerator[] generators, Options opts, params string[] stages)
    {
        var syntaxTrees = opts.Sources
                              .Select(x =>
                              {
                                  var tree = CSharpSyntaxTree.ParseText(x, path: "Program.cs");
                                  var options = new CSharpParseOptions(opts.LanguageVersion).WithFeatures(opts.Features);
                                  return tree.WithRootAndOptions(tree.GetRoot(), options);
                              });

        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                                  .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                                  .Concat([
                                      ..generators.Select(x => MetadataReference.CreateFromFile(x.GetType().Assembly.Location)),
                                      MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location),
                                      MetadataReference.CreateFromFile(typeof(MessageAbstractionsGenerator).Assembly.Location),
                                      MetadataReference.CreateFromFile(typeof(DisplayAttribute).Assembly.Location),
                                      MetadataReference.CreateFromFile(typeof(GeneratedCodeAttribute).Assembly.Location),
                                  ]);

        var compilation = CSharpCompilation.Create(
            "generator",
            syntaxTrees,
            references,
            new(OutputKind.DynamicallyLinkedLibrary));

        var (runResult, diagnostics) = RunGeneratorAndAssertOutput(generators, opts, compilation, stages);

        var combinedDiagnostics = runResult.Diagnostics.AddRange(diagnostics);

        return (combinedDiagnostics, runResult.GeneratedTrees);
    }

    private static (GeneratorDriverRunResult RunResult, ImmutableArray<Diagnostic> Diagnostics) RunGeneratorAndAssertOutput(
        IIncrementalGenerator[] generators,
        Options options,
        CSharpCompilation compilation,
        string[] stages,
        bool assertOutput = true)
    {
        var opts = new GeneratorDriverOptions(
            IncrementalGeneratorOutputKind.None,
            true);

        GeneratorDriver driver =
            CSharpGeneratorDriver.Create(
                generators.Select(x => x.AsSourceGenerator()),
                driverOptions: opts,
                optionsProvider: options.OptsProvider,
                parseOptions: new CSharpParseOptions(options.LanguageVersion).WithFeatures(options.Features));

        var clone = compilation.Clone();

        // Run twice, once with a clone of the compilation
        // Note that we store the returned drive value, as it contains cached previous outputs
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var runResult = driver.GetRunResult();

        if (assertOutput)
        {
            // Run with a clone of the compilation
            var runResult2 = driver
                             .RunGenerators(clone)
                             .GetRunResult();

            AssertRunsEqual(runResult, runResult2, stages);

            // verify the second run only generated cached source outputs
            Assert.That(
                runResult2.Results[0]
                          .TrackedOutputSteps
                          .SelectMany(x => x.Value) // step executions
                          .SelectMany(x => x.Outputs), // execution results
                Is.All.Matches(((object Value, IncrementalStepRunReason Reason) x) => x.Reason == IncrementalStepRunReason.Cached));
        }

        return (runResult, outputCompilation.GetDiagnostics());
    }

    private static void AssertRunsEqual(GeneratorDriverRunResult runResult1, GeneratorDriverRunResult runResult2, string[] trackingNames)
    {
        // We're given all the tracking names, but not all the stages have necessarily executed so filter
        var trackedSteps1 = GetTrackedSteps(runResult1, trackingNames);
        var trackedSteps2 = GetTrackedSteps(runResult2, trackingNames);

        // These should be the same
        Assert.That(trackedSteps1, Has.Count.EqualTo(trackedSteps2.Count));
        Assert.That(trackedSteps1.Keys, Is.SupersetOf(trackedSteps2.Keys));

        foreach (var trackedStep in trackedSteps1)
        {
            var trackingName = trackedStep.Key;
            var runSteps1 = trackedStep.Value;
            var runSteps2 = trackedSteps2[trackingName];
            AssertEqual(runSteps1, runSteps2, trackingName);
        }

        static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(
            GeneratorDriverRunResult runResult, string[] trackingNames) =>
            runResult.Results[0]
                     .TrackedSteps
                     .Where(step => trackingNames.Contains(step.Key))
                     .ToDictionary(x => x.Key, x => x.Value);
    }

    private static void AssertEqual(
        ImmutableArray<IncrementalGeneratorRunStep> runSteps1,
        ImmutableArray<IncrementalGeneratorRunStep> runSteps2,
        string stepName)
    {
        Assert.That(runSteps1, Has.Length.EqualTo(runSteps2.Length));

        for (var i = 0; i < runSteps1.Length; i++)
        {
            var runStep1 = runSteps1[i];
            var runStep2 = runSteps2[i];

            // The outputs should be equal between different runs
            var outputs1 = runStep1.Outputs.Select(x => x.Value);
            var outputs2 = runStep2.Outputs.Select(x => x.Value);

            Assert.That(outputs1, Is.EqualTo(outputs2), $"because {stepName} should produce cacheable outputs");

            // Therefore, on the second run the results should always be cached or unchanged!
            // - Unchanged is when the _input_ has changed, but the output hasn't
            // - Cached is when the input has not changed, so the cached output is used
            Assert.That(runStep2.Outputs,
                        Is.All.Matches(((object Value, IncrementalStepRunReason Reason) x) => x.Reason == IncrementalStepRunReason.Cached || x.Reason == IncrementalStepRunReason.Unchanged),
                        $"{stepName} expected to have reason {IncrementalStepRunReason.Cached} or {IncrementalStepRunReason.Unchanged}");

            // Make sure we're not using anything we shouldn't
            AssertObjectGraph(runStep1, stepName);
            AssertObjectGraph(runStep2, stepName);
        }

        static void AssertObjectGraph(IncrementalGeneratorRunStep runStep, string stepName)
        {
            var visited = new HashSet<object>();

            foreach (var (obj, _) in runStep.Outputs)
            {
                Visit(obj);
            }

            void Visit(object? node)
            {
                if (node is null || !visited.Add(node))
                {
                    return;
                }

                Assert.That(node, Is.Not.InstanceOf<Compilation>()
                                    .And.Not.InstanceOf<ISymbol>()
                                    .And.Not.InstanceOf<SyntaxNode>(),
                            $"{stepName} shouldn't contain banned symbols");

                var type = node.GetType();
                if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                {
                    return;
                }

                if (node is IEnumerable collection and not string)
                {
                    foreach (var element in collection)
                    {
                        Visit(element);
                    }

                    return;
                }

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var fieldValue = field.GetValue(node);
                    Visit(fieldValue);
                }
            }
        }
    }

    private static string[] GetTrackingNames<TTrackingNames>()
        => typeof(TTrackingNames)
           .GetFields()
           .Where(fi => fi is { IsLiteral: true, IsInitOnly: false } && fi.FieldType == typeof(string))
           .Select(x => (string?)x.GetRawConstantValue()!)
           .Where(x => !string.IsNullOrEmpty(x))
           .ToArray();

    private sealed class OptionsProvider(AnalyzerConfigOptions options) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => options;
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;
    }

    internal sealed class DictionaryAnalyzerOptions(Dictionary<string, string> properties) : AnalyzerConfigOptions
    {
        public static DictionaryAnalyzerOptions Empty { get; } = new(new());

        public override bool TryGetValue(string key, out string value)
            => properties.TryGetValue(key, out value!);
    }

    public sealed record Options
    {
        public Options(params string[] sources)
            : this(LanguageVersion.Default, null, null, sources, null)
        {
        }

        public Options(Dictionary<string, string> options, params string[] sources)
            : this(LanguageVersion.Default, options, null, sources, null)
        {
        }

        public Options(LanguageVersion languageVersion, Dictionary<string, string> options, params string[] sources)
            : this(languageVersion, options, null, sources, null)
        {
        }

        public Options(Dictionary<string, string> options, Dictionary<string, string> features, params string[] sources)
            : this(LanguageVersion.Default, options, features, sources, null)
        {
        }

        public Options(LanguageVersion languageVersion, Dictionary<string, string> options, Dictionary<string, string> features, params string[] sources)
            : this(languageVersion, options, features, sources, null)
        {
        }

        private Options(LanguageVersion LanguageVersion,
                        Dictionary<string, string>? AnalyzerOptions,
                        Dictionary<string, string>? Features,
                        string[] Sources,
                        string[]? Stages)
        {
            this.LanguageVersion = LanguageVersion;
            this.AnalyzerOptions = AnalyzerOptions;
            this.Features = Features;
            this.Sources = Sources;
            this.Stages = Stages;
        }

        public AnalyzerConfigOptionsProvider? OptsProvider =>
            AnalyzerOptions is not null ? new OptionsProvider(new DictionaryAnalyzerOptions(AnalyzerOptions)) : null;

        public LanguageVersion LanguageVersion { get; }
        private Dictionary<string, string>? AnalyzerOptions { get; }
        public Dictionary<string, string>? Features { get; }
        public string[] Sources { get; }
        public string[]? Stages { get; }
    }
}
