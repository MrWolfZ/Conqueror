using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// it makes sense for these classes to be in the same file
#pragma warning disable SA1402

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class HttpCommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : class
    {
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions? serializerOptions;

        public HttpCommandHandler(ResolvedHttpClientOptions options)
        {
            httpClient = options.HttpClient;
            serializerOptions = options.JsonSerializerOptions;
        }

        public async Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            var regex = new Regex("Command$");
            var routeCommandName = regex.Replace(typeof(TCommand).Name, string.Empty);
            var response = await httpClient.PostAsJsonAsync($"/api/commands/{routeCommandName}", command, serializerOptions, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                var content = await response.BufferAndReadContent();
                throw new HttpCommandFailedException($"command of type {typeof(TCommand).Name} failed: {content}", response);
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(serializerOptions, cancellationToken);
            return result!;
        }
    }

    internal sealed class HttpCommandHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : class
    {
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions? serializerOptions;

        public HttpCommandHandler(ResolvedHttpClientOptions options)
        {
            httpClient = options.HttpClient;
            serializerOptions = options.JsonSerializerOptions;
        }

        public async Task ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            var regex = new Regex("Command$");
            var routeCommandName = regex.Replace(typeof(TCommand).Name, string.Empty);
            var response = await httpClient.PostAsJsonAsync($"/api/commands/{routeCommandName}", command, serializerOptions, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                var content = await response.BufferAndReadContent();
                throw new HttpCommandFailedException($"command of type {typeof(TCommand).Name} failed: {content}", response);
            }
        }
    }
}
