namespace Conqueror.Middleware.Logging.Tests;

internal sealed class TruncatedLoggingException(Exception wrapped) : Exception(wrapped.Message)
{
    public override string? StackTrace => wrapped.StackTrace;

    public override string ToString()
    {
        var wrappedToString = wrapped.ToString();

        // during testing we found that the output of exception stack traces is not stable, but
        // we need them to be stable for snapshot testing; we observed that the instability is
        // usually after the first few lines of the stack trace, so we truncate the trace to
        // still be able to assert that something is being logged but without the unstable part
        var numOfLinesToKeep = 6;
        var numOfNewLinesFound = 0;
        var currentIndex = 0;

        while (currentIndex < wrappedToString.Length && numOfNewLinesFound < numOfLinesToKeep)
        {
            var nextIndex = wrappedToString.IndexOf("\n", currentIndex + 1, StringComparison.InvariantCulture);

            if (nextIndex == -1)
            {
                break;
            }

            currentIndex = nextIndex;
            numOfNewLinesFound += 1;
        }

        return wrappedToString[..currentIndex].TrimEnd();
    }
}
