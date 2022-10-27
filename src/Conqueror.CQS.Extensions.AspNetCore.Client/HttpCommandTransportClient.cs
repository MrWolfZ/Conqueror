using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.Common;
using Conqueror.CQS.Extensions.AspNetCore.Common;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class HttpCommandTransportClient : ICommandTransportClient
    {
        private readonly IConquerorContextAccessor? conquerorContextAccessor;
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions? serializerOptions;

        public HttpCommandTransportClient(ResolvedHttpClientOptions options, IConquerorContextAccessor? conquerorContextAccessor)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            httpClient = options.HttpClient;
            serializerOptions = options.JsonSerializerOptions;
        }

        public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
            where TCommand : class
        {
            using var content = JsonContent.Create(command, null, serializerOptions);

            if (conquerorContextAccessor?.ConquerorContext?.Items is { Count: > 0 } contextItems)
            {
                content.Headers.Add(HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(contextItems));
            }

            var regex = new Regex("Command$");
            var routeCommandName = regex.Replace(typeof(TCommand).Name, string.Empty);
            var response = await httpClient.PostAsync(new Uri($"/api/commands/{routeCommandName}", UriKind.Relative), content, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                var responseContent = await response.BufferAndReadContent();
                throw new HttpCommandFailedException($"command of type {typeof(TCommand).Name} failed: {responseContent}", response);
            }

            if (conquerorContextAccessor?.ConquerorContext is { } ctx && response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values))
            {
                var parsedContextItems = ContextValueFormatter.Parse(values);
                ctx.AddOrReplaceItems(parsedContextItems);
            }

            if (typeof(TResponse) == typeof(UnitCommandResponse))
            {
                return (TResponse)(object)UnitCommandResponse.Instance;
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(serializerOptions, cancellationToken);
            return result!;
        }
    }
}
