using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Conqueror.CQS")]
[assembly: InternalsVisibleTo("Conqueror.CQS.Common")]
[assembly: InternalsVisibleTo("Conqueror.CQS.Tests")]
[assembly: InternalsVisibleTo("Conqueror.Eventing")]
[assembly: InternalsVisibleTo("Conqueror.Eventing.Tests")]
[assembly: InternalsVisibleTo("Conqueror.Streaming")]
[assembly: InternalsVisibleTo("Conqueror.Streaming.Tests")]

// TODO: remove once libraries below are refactored to not do their own dynamic type generation
[assembly: InternalsVisibleTo("Conqueror.Streaming.Transport.Http.Client")]
