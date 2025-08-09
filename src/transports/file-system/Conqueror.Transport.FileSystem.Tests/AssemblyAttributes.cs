using Conqueror.Transport.FileSystem.Tests;

[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[assembly: Parallelizable(ParallelScope.Children)]

[assembly: FileSystemTransportTestLoggingHook]
