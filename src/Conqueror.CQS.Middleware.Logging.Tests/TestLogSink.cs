using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests;

public sealed class TestLogSink
{
    public List<(string CategoryName, LogLevel LogLevel, string Message)> LogEntries { get; } = [];
}
