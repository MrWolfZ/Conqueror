using Microsoft.Extensions.Logging;

namespace Conqueror.Transport.ConformityTests;

public interface ITransportConformityTestHost : IAsyncDisposable
{
    CancellationToken TestTimeoutToken { get; }

    TimeSpan AssertionTimeout { get; }

    TimeSpan ShortDelay { get; }

    int AssertionTimeoutInMs { get; }

    ILogger Logger { get; }
}
