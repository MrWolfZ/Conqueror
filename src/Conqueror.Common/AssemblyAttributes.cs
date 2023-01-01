using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Conqueror.CQS")]
[assembly: InternalsVisibleTo("Conqueror.CQS.Common")]
[assembly: InternalsVisibleTo("Conqueror.CQS.Tests")]
[assembly: InternalsVisibleTo("Conqueror.Eventing")]
[assembly: InternalsVisibleTo("Conqueror.Eventing.Tests")]
[assembly: InternalsVisibleTo("Conqueror.Streaming.Interactive")]
[assembly: InternalsVisibleTo("Conqueror.Streaming.Interactive.Tests")]
[assembly: InternalsVisibleTo("Conqueror.Streaming.Reactive")]
[assembly: InternalsVisibleTo("Conqueror.Streaming.Reactive.Tests")]

// TODO: remove once libraries below are refactored to not do their own dynamic type generation
[assembly: InternalsVisibleTo("Conqueror.Streaming.Interactive.Transport.Http.Client")]
