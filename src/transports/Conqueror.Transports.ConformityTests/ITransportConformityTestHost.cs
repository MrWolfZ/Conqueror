using Microsoft.Extensions.Logging;

namespace Conqueror.Transports.ConformityTests;

public interface ITransportConformityTestHost : IAsyncDisposable
{
    CancellationToken TestTimeoutToken { get; }

    TimeSpan AssertionTimeout { get; }

    int AssertionTimeoutInMs { get; }

    ILogger Logger { get; }
}
