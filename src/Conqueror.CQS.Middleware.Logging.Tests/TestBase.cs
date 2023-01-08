using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests
{
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "resources are disposed in test teardown")]
    public abstract class TestBase
    {
        private IHost? host;
        private CancellationTokenSource? timeoutCancellationTokenSource;

        protected IHost Host
        {
            get
            {
                if (host == null)
                {
                    throw new InvalidOperationException("test fixture must be initialized before using host");
                }

                return host;
            }
        }

        protected virtual TimeSpan TestTimeout => TimeSpan.FromSeconds(2);

        protected CancellationToken TestTimeoutToken => TimeoutCancellationTokenSource.Token;

        protected TestLogSink LogSink => Resolve<TestLogSink>();

        private CancellationTokenSource TimeoutCancellationTokenSource
        {
            get
            {
                if (timeoutCancellationTokenSource == null)
                {
                    throw new InvalidOperationException("test fixture must be initialized before timeout cancellation token source");
                }

                return timeoutCancellationTokenSource;
            }
        }

        [SetUp]
        public void SetUp()
        {
            timeoutCancellationTokenSource = new();

            var hostBuilder = new HostBuilder().ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Trace))
                                               .ConfigureServices(ConfigureServices)
                                               .ConfigureServices((_, services) => services.AddSingleton<TestLoggerProvider>()
                                                                                           .AddSingleton<ILoggerProvider>(p => p.GetRequiredService<TestLoggerProvider>())
                                                                                           .AddSingleton<TestLogSink>());

            host = hostBuilder.Build();

            if (!Debugger.IsAttached)
            {
                TimeoutCancellationTokenSource.CancelAfter(TestTimeout);
            }
        }

        [TearDown]
        public void TearDown()
        {
            timeoutCancellationTokenSource?.Cancel();
            host?.Dispose();
            timeoutCancellationTokenSource?.Dispose();
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected T Resolve<T>()
            where T : notnull => Host.Services.GetRequiredService<T>();

        protected void AssertLogEntryMatches(LogLevel logLevel, Regex regex, string? categoryName = null, int nrOfTimes = 1)
        {
            Assert.That(LogSink.LogEntries, Has.Exactly(nrOfTimes).Matches<(string CategoryName, LogLevel LogLevel, string Message)>(t => (categoryName is null || t.CategoryName == categoryName) &&
                                                                                                                                          t.LogLevel == logLevel &&
                                                                                                                                          regex.IsMatch(t.Message)));
        }

        protected void AssertLogEntryContains(LogLevel logLevel, string fragment, string? categoryName = null, int nrOfTimes = 1)
        {
            Assert.That(LogSink.LogEntries, Has.Exactly(nrOfTimes).Matches<(string CategoryName, LogLevel LogLevel, string Message)>(t => (categoryName is null || t.CategoryName == categoryName) &&
                                                                                                                                          t.LogLevel == logLevel &&
                                                                                                                                          t.Message.Contains(fragment)));
        }

        protected void AssertNoLogEntryMatches(LogLevel logLevel, Regex regex)
        {
            Assert.That(LogSink.LogEntries, Has.Exactly(0).Matches<(string CategoryName, LogLevel LogLevel, string Message)>(t => t.LogLevel == logLevel && regex.IsMatch(t.Message)));
        }

        protected void AssertNoLogEntryContains(LogLevel logLevel, string fragment)
        {
            Assert.That(LogSink.LogEntries, Has.Exactly(0).Matches<(string CategoryName, LogLevel LogLevel, string Message)>(t => t.LogLevel == logLevel && t.Message.Contains(fragment)));
        }
    }
}
