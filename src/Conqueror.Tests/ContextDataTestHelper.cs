namespace Conqueror.Tests;

[SuppressMessage(
    "Roslynator",
    "RCS1250:Use implicit/explicit object creation",
    Justification = "we prefer the collection expression syntax here"
)]
internal sealed class ContextDataTestHelper
{
    private const string TestKey = "TestKey";

    public static IEnumerable<TestCaseData> GenerateContextDataTestCases(ExecutionOrderItem[] executionOrder)
    {
        foreach (var dataType in new[] { ContextDataType.String, ContextDataType.Object })
        {
            foreach (var testCaseData in GenerateDownstreamTestCaseData(dataType, executionOrder))
            {
                yield return new ConquerorContextDataTestCase(DataDirection.Downstream, testCaseData);
            }

            foreach (var testCaseData in GenerateUpstreamTestCaseData(dataType, executionOrder))
            {
                yield return new ConquerorContextDataTestCase(DataDirection.Upstream, testCaseData);
            }

            foreach (var testCaseData in GenerateBidirectionalTestCaseData(dataType, executionOrder))
            {
                yield return new ConquerorContextDataTestCase(DataDirection.Bidirectional, testCaseData);
            }
        }
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateDownstreamTestCaseData(
        string dataType,
        ExecutionOrderItem[] executionOrder
    )
    {
        var allLocations = executionOrder.Select(t => t.Location).ToList();

        // setting tests

        for (var i = 0; i < executionOrder.Length; i += 1)
        {
            var (contextDepth, depthInstance, location) = executionOrder[i];
            var whereDataShouldBeAccessible = executionOrder[i..]
                .Where(t =>
                    t.ContextDepth > contextDepth
                    || (t.ContextDepth == contextDepth && t.DepthInstance == depthInstance)
                )
                .Select(t => t.Location)
                .ToList();

            yield return
            [
                new(
                    dataType,
                    location,
                    DataRemovalLocation: null,
                    whereDataShouldBeAccessible,
                    allLocations.Except(whereDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase).ToList()
                ),
            ];
        }

        // overwrite tests

        foreach (var overWriteDataType in new[] { ContextDataType.String, ContextDataType.Object })
        {
            for (var i = 0; i < executionOrder.Length - 1; i += 1)
            {
                var (initialContextDepth, initialDepthInstance, initialDataSettingLocation) = executionOrder[i];

                for (var j = i + 1; j < executionOrder.Length; j += 1)
                {
                    var (overwrittenContextDepth, overwrittenDepthInstance, overwrittenDataSettingLocation) =
                        executionOrder[j];

                    var whereOverwrittenDataShouldBeAccessible = executionOrder[j..]
                        .Where(t =>
                            t.ContextDepth > overwrittenContextDepth
                            || (
                                t.ContextDepth == overwrittenContextDepth && t.DepthInstance == overwrittenDepthInstance
                            )
                        )
                        .Select(t => t.Location)
                        .ToList();

                    var whereInitialDataShouldBeAccessible = executionOrder[i..]
                        .Where(t =>
                            t.ContextDepth > initialContextDepth
                            || (t.ContextDepth == initialContextDepth && t.DepthInstance == initialDepthInstance)
                        )
                        .Select(t => t.Location)
                        .Except(whereOverwrittenDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    yield return
                    [
                        new(
                            dataType,
                            initialDataSettingLocation,
                            DataRemovalLocation: null,
                            whereInitialDataShouldBeAccessible,
                            allLocations
                                .Except(whereInitialDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase)
                                .ToList()
                        ),
                        new(
                            overWriteDataType,
                            overwrittenDataSettingLocation,
                            DataRemovalLocation: null,
                            whereOverwrittenDataShouldBeAccessible,
                            allLocations
                                .Except(whereOverwrittenDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase)
                                .ToList()
                        ),
                    ];
                }
            }
        }

        // removal tests

        for (var i = 0; i < executionOrder.Length - 1; i += 1)
        {
            var (contextDepth, depthInstance, dataSettingLocation) = executionOrder[i];

            for (var j = i + 1; j < executionOrder.Length; j += 1)
            {
                var (removalContextDepth, removalDepthInstance, removalLocation) = executionOrder[j];

                var whereDataShouldBeRemoved = executionOrder[j..]
                    .Where(t =>
                        t.ContextDepth > removalContextDepth
                        || (t.ContextDepth == removalContextDepth && t.DepthInstance == removalDepthInstance)
                    )
                    .Select(t => t.Location)
                    .ToList();

                var whereDataShouldBeAccessible = executionOrder[i..]
                    .Where(t =>
                        t.ContextDepth > contextDepth
                        || (t.ContextDepth == contextDepth && t.DepthInstance == depthInstance)
                    )
                    .Select(t => t.Location)
                    .Except(whereDataShouldBeRemoved, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                yield return
                [
                    new(
                        dataType,
                        dataSettingLocation,
                        removalLocation,
                        whereDataShouldBeAccessible,
                        allLocations.Except(whereDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase).ToList()
                    ),
                ];
            }
        }
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateUpstreamTestCaseData(
        string dataType,
        ExecutionOrderItem[] executionOrder
    )
    {
        var allLocations = executionOrder.Select(t => t.Location).ToList();

        // setting tests

        for (var i = 0; i < executionOrder.Length; i += 1)
        {
            var (contextDepth, depthInstance, location) = executionOrder[i];
            var whereDataShouldBeAccessible = executionOrder[i..]
                .Where(t => t.ContextDepth <= contextDepth && t.DepthInstance <= depthInstance)
                .Select(t => t.Location)
                .ToList();

            yield return
            [
                new(
                    dataType,
                    location,
                    DataRemovalLocation: null,
                    whereDataShouldBeAccessible,
                    allLocations.Except(whereDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase).ToList()
                ),
            ];
        }

        // overwrite tests

        foreach (var overWriteDataType in new[] { ContextDataType.String, ContextDataType.Object })
        {
            for (var i = 0; i < executionOrder.Length - 1; i += 1)
            {
                var (initialContextDepth, initialDepthInstance, initialDataSettingLocation) = executionOrder[i];

                for (var j = i + 1; j < executionOrder.Length; j += 1)
                {
                    var (overwrittenContextDepth, overwrittenDepthInstance, overwrittenDataSettingLocation) =
                        executionOrder[j];

                    var whereOverwrittenDataShouldBeAccessible = executionOrder[j..]
                        .Where(t =>
                            t.ContextDepth <= overwrittenContextDepth && t.DepthInstance <= overwrittenDepthInstance
                        )
                        .Select(t => t.Location)
                        .ToList();

                    var whereInitialDataShouldBeAccessible = executionOrder[i..]
                        .Where(t => t.ContextDepth <= initialContextDepth && t.DepthInstance <= initialDepthInstance)
                        .Select(t => t.Location)
                        .Except(whereOverwrittenDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    yield return
                    [
                        new(
                            dataType,
                            initialDataSettingLocation,
                            DataRemovalLocation: null,
                            whereInitialDataShouldBeAccessible,
                            allLocations
                                .Except(whereInitialDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase)
                                .ToList()
                        ),
                        new(
                            overWriteDataType,
                            overwrittenDataSettingLocation,
                            DataRemovalLocation: null,
                            whereOverwrittenDataShouldBeAccessible,
                            allLocations
                                .Except(whereOverwrittenDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase)
                                .ToList()
                        ),
                    ];
                }
            }
        }

        // removal tests

        for (var i = 0; i < executionOrder.Length - 1; i += 1)
        {
            var (settingContextDepth, settingDepthInstance, dataSettingLocation) = executionOrder[i];

            for (var j = i + 1; j < executionOrder.Length; j += 1)
            {
                var (removalContextDepth, removalDepthInstance, removalLocation) = executionOrder[j];

                var whereDataShouldBeRemoved =
                    settingContextDepth < removalContextDepth || settingDepthInstance < removalDepthInstance
                        ? []
                        : executionOrder[j..]
                            .Where(t => t.ContextDepth <= removalContextDepth)
                            .Select(t => t.Location)
                            .ToList();

                var whereDataShouldBeAccessible = executionOrder[i..]
                    .Where(t => t.ContextDepth <= settingContextDepth && t.DepthInstance <= settingDepthInstance)
                    .Select(t => t.Location)
                    .Except(whereDataShouldBeRemoved, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                yield return
                [
                    new(
                        dataType,
                        dataSettingLocation,
                        removalLocation,
                        whereDataShouldBeAccessible,
                        allLocations.Except(whereDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase).ToList()
                    ),
                ];
            }
        }
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateBidirectionalTestCaseData(
        string dataType,
        ExecutionOrderItem[] executionOrder
    )
    {
        var allLocations = executionOrder.Select(t => t.Location).ToList();

        // setting tests

        for (var i = 0; i < executionOrder.Length; i += 1)
        {
            var (_, _, location) = executionOrder[i];
            var whereDataShouldBeAccessible = executionOrder[i..].Select(t => t.Location).ToList();

            yield return
            [
                new(
                    dataType,
                    location,
                    DataRemovalLocation: null,
                    whereDataShouldBeAccessible,
                    allLocations.Except(whereDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase).ToList()
                ),
            ];
        }

        // overwrite tests

        foreach (var overWriteDataType in new[] { ContextDataType.String, ContextDataType.Object })
        {
            for (var i = 0; i < executionOrder.Length - 1; i += 1)
            {
                var (_, _, initialDataSettingLocation) = executionOrder[i];

                for (var j = i + 1; j < executionOrder.Length; j += 1)
                {
                    var (_, _, overwrittenDataSettingLocation) = executionOrder[j];

                    var whereOverwrittenDataShouldBeAccessible = executionOrder[j..].Select(t => t.Location).ToList();

                    var whereInitialDataShouldBeAccessible = executionOrder[i..]
                        .Select(t => t.Location)
                        .Except(whereOverwrittenDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    yield return
                    [
                        new(
                            dataType,
                            initialDataSettingLocation,
                            DataRemovalLocation: null,
                            whereInitialDataShouldBeAccessible,
                            allLocations
                                .Except(whereInitialDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase)
                                .ToList()
                        ),
                        new(
                            overWriteDataType,
                            overwrittenDataSettingLocation,
                            DataRemovalLocation: null,
                            whereOverwrittenDataShouldBeAccessible,
                            allLocations
                                .Except(whereOverwrittenDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase)
                                .ToList()
                        ),
                    ];
                }
            }
        }

        // removal tests

        for (var i = 0; i < executionOrder.Length - 1; i += 1)
        {
            var (_, _, dataSettingLocation) = executionOrder[i];

            for (var j = i + 1; j < executionOrder.Length; j += 1)
            {
                var (_, _, removalLocation) = executionOrder[j];

                var whereDataShouldBeRemoved = executionOrder[j..].Select(t => t.Location).ToList();

                var whereDataShouldBeAccessible = executionOrder[i..]
                    .Select(t => t.Location)
                    .Except(whereDataShouldBeRemoved, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                yield return
                [
                    new(
                        dataType,
                        dataSettingLocation,
                        removalLocation,
                        whereDataShouldBeAccessible,
                        allLocations.Except(whereDataShouldBeAccessible, StringComparer.OrdinalIgnoreCase).ToList()
                    ),
                ];
            }
        }
    }

    [SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1202:Elements should be ordered by access",
        Justification = "the order makes sense here"
    )]
    public static void SetAndObserveContextData(
        ConquerorContext ctx,
        TestDataInstructions testDataInstructions,
        TestObservations testObservations,
        string location
    )
    {
        // ReSharper disable RedundantArgumentDefaultValue
        foreach (
            var (key, value, _) in testDataInstructions.DownstreamDataToSet.Where(t =>
                string.Equals(t.Location, location, StringComparison.Ordinal)
            )
        )
        {
            ctx.InProcessData.Set(key, value, ConquerorContextDataFlowDirection.Downstream);
        }

        foreach (
            var (key, _) in testDataInstructions.DownstreamDataToRemove.Where(t =>
                string.Equals(t.Location, location, StringComparison.Ordinal)
            )
        )
        {
            _ = ctx.InProcessData.Remove(key, ConquerorContextDataFlowDirection.Downstream);
        }

        foreach (
            var (key, value, _) in testDataInstructions.UpstreamDataToSet.Where(t =>
                string.Equals(t.Location, location, StringComparison.Ordinal)
            )
        )
        {
            ctx.InProcessData.Set(key, value, ConquerorContextDataFlowDirection.Upstream);
        }

        foreach (
            var (key, _) in testDataInstructions.UpstreamDataToRemove.Where(t =>
                string.Equals(t.Location, location, StringComparison.Ordinal)
            )
        )
        {
            _ = ctx.InProcessData.Remove(key, ConquerorContextDataFlowDirection.Upstream);
        }

        foreach (
            var (key, value, _) in testDataInstructions.BidirectionalDataToSet.Where(t =>
                string.Equals(t.Location, location, StringComparison.Ordinal)
            )
        )
        {
            ctx.InProcessData.Set(key, value, ConquerorContextDataFlowDirection.Bidirectional);
        }

        foreach (
            var (key, _) in testDataInstructions.BidirectionalDataToRemove.Where(t =>
                string.Equals(t.Location, location, StringComparison.Ordinal)
            )
        )
        {
            _ = ctx.InProcessData.Remove(key, ConquerorContextDataFlowDirection.Bidirectional);
        }

        foreach (var (key, value) in ctx.InProcessData.GetAll(ConquerorContextDataFlowDirection.Downstream))
        {
            testObservations.ObservedDownstreamData.Add((key, value, location));
        }

        if (ctx.InProcessData.Get<object>(TestKey, ConquerorContextDataFlowDirection.Downstream) is { } downstreamValue)
        {
            testObservations.ObservedDownstreamData.Add((TestKey, downstreamValue, location));
        }

        foreach (var (key, value) in ctx.InProcessData.GetAll(ConquerorContextDataFlowDirection.Upstream))
        {
            testObservations.ObservedUpstreamData.Add((key, value, location));
        }

        if (ctx.InProcessData.Get<object>(TestKey, ConquerorContextDataFlowDirection.Upstream) is { } upstreamValue)
        {
            testObservations.ObservedUpstreamData.Add((TestKey, upstreamValue, location));
        }

        foreach (var (key, value) in ctx.InProcessData.GetAll(ConquerorContextDataFlowDirection.Bidirectional))
        {
            testObservations.ObservedBidirectionalData.Add((key, value, location));
        }

        if (
            ctx.InProcessData.Get<object>(TestKey, ConquerorContextDataFlowDirection.Bidirectional) is
            { } bidirectionalValue
        )
        {
            testObservations.ObservedBidirectionalData.Add((TestKey, bidirectionalValue, location));
        }

        // ReSharper enable RedundantArgumentDefaultValue
    }

    [SuppressMessage(
        "Critical Code Smell",
        "S3218:Inner class members should not shadow outer class \"static\" or type members",
        Justification = "The name makes sense and there is little risk of confusing a property and a class."
    )]
    [SuppressMessage(
        "ReSharper",
        "MemberHidesStaticFromOuterClass",
        Justification = "The name makes sense and there is little risk of confusing a property and a class."
    )]
    public sealed record ConquerorContextDataTestCase(
        string DataDirection,
        List<ConquerorContextDataTestCaseData> TestData
    )
    {
        public static implicit operator TestCaseData(ConquerorContextDataTestCase testCase)
        {
            var testName = new StringBuilder()
                .Append(testCase.DataDirection)
                .Append($",data:{string.Join(',', testCase.TestData)}")
                .ToString();

            return new(testCase) { TestName = testName };
        }
    }

    [SuppressMessage(
        "Critical Code Smell",
        "S3218:Inner class members should not shadow outer class \"static\" or type members",
        Justification = "The name makes sense and there is little risk of confusing a property and a class."
    )]
    [SuppressMessage(
        "ReSharper",
        "MemberHidesStaticFromOuterClass",
        Justification = "The name makes sense and there is little risk of confusing a property and a class."
    )]
    public sealed record ConquerorContextDataTestCaseData(
        string DataType,
        string DataSettingLocation,
        string? DataRemovalLocation,
        IReadOnlyCollection<string> LocationsWhereDataShouldBeAccessible,
        IReadOnlyCollection<string> LocationsWhereDataShouldNotBeAccessible
    )
    {
        public override string ToString()
        {
            var sb = new StringBuilder().Append(DataType).Append($",setLoc:{DataSettingLocation}");

            if (DataRemovalLocation is not null)
            {
                _ = sb.Append($",remLoc:{DataRemovalLocation}");
            }

            return sb.ToString();
        }
    }

    public static class DataDirection
    {
        public const string Downstream = nameof(Downstream);
        public const string Upstream = nameof(Upstream);
        public const string Bidirectional = nameof(Bidirectional);
    }

    public sealed record ExecutionOrderItem(int ContextDepth, int DepthInstance, string Location);

    [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "intentional")]
    public static class ContextDataType
    {
        public const string String = nameof(String);
        public const string Object = nameof(Object);
    }

    public sealed class TestDataInstructions
    {
        public List<(string Key, object Value, string Location)> DownstreamDataToSet { get; } = [];

        public List<(string Key, string Location)> DownstreamDataToRemove { get; } = [];

        public List<(string Key, object Value, string Location)> UpstreamDataToSet { get; } = [];

        public List<(string Key, string Location)> UpstreamDataToRemove { get; } = [];

        public List<(string Key, object Value, string Location)> BidirectionalDataToSet { get; } = [];

        public List<(string Key, string Location)> BidirectionalDataToRemove { get; } = [];
    }

    public sealed class TestObservations
    {
        public List<(string Key, object Value, string Location)> ObservedDownstreamData { get; } = [];

        public List<(string Key, object Value, string Location)> ObservedUpstreamData { get; } = [];

        public List<(string Key, object Value, string Location)> ObservedBidirectionalData { get; } = [];
    }
}
