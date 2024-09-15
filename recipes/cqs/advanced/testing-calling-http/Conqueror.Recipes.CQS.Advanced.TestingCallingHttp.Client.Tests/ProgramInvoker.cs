using System.Diagnostics;
using System.Reflection;

namespace Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Client.Tests;

public static class ProgramInvoker
{
    public static async Task<string> Invoke(Action<IServiceCollection> configureServices, params string[] args)
    {
        var consoleOut = Console.Out;

        try
        {
            // subscribe to hosting diagnostics event to allow configuring services
            using var d = DiagnosticListener.AllListeners.Subscribe(new HostingListener(builder => builder.ConfigureServices(configureServices)));

            await using var stringWriter = new StringWriter();

            Console.SetOut(stringWriter);

            // call the implicitly created entry point
            var result = typeof(Program).GetMethod("<Main>$", BindingFlags.NonPublic | BindingFlags.Static)!
                                        .Invoke(null, [args]);

            if (result is Task t)
            {
                await t;
            }

            return stringWriter.ToString();
        }
        finally
        {
            Console.SetOut(consoleOut);
        }
    }

    private sealed class HostingListener(Action<IHostBuilder> configure) : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        private IDisposable? disposable;

        public void OnError(Exception error) => throw error;

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == "HostBuilding")
            {
                configure((IHostBuilder)value.Value!);
            }
        }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name == "Microsoft.Extensions.Hosting")
            {
                disposable = value.Subscribe(this);
            }
        }

        public void OnCompleted() => disposable?.Dispose();
    }
}
