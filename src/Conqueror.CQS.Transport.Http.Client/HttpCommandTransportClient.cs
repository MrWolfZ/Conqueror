using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.Common;
using Conqueror.CQS.Transport.Http.Common;

namespace Conqueror.CQS.Transport.Http.Client
{
    internal sealed class HttpCommandTransportClient : ICommandTransportClient
    {
        private readonly ICommandContextAccessor commandContextAccessor;

        private readonly IConquerorContextAccessor conquerorContextAccessor;

        public HttpCommandTransportClient(ResolvedHttpClientOptions options, IConquerorContextAccessor conquerorContextAccessor, ICommandContextAccessor commandContextAccessor)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.commandContextAccessor = commandContextAccessor;
            Options = options;
        }

        public ResolvedHttpClientOptions Options { get; }

        public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
            where TCommand : class
        {
            var attribute = typeof(TCommand).GetCustomAttribute<HttpCommandAttribute>()!;

            using var content = JsonContent.Create(command, null, Options.JsonSerializerOptions);

            var path = Options.CommandPathConvention?.GetCommandPath(typeof(TCommand), attribute) ?? DefaultHttpCommandPathConvention.Instance.GetCommandPath(typeof(TCommand), attribute);

            using var message = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new(Options.BaseAddress, path),
                Content = content,
            };

            SetHeaders(message.Headers);

            TracingHelper.SetTraceParentHeaderForTestClient(message.Headers, Options.HttpClient);

            var response = await Options.HttpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                var responseContent = await response.BufferAndReadContent().ConfigureAwait(false);
                throw new HttpCommandFailedException($"command of type {typeof(TCommand).Name} failed with status code {response.StatusCode} and response content: {responseContent}", response);
            }

            if (conquerorContextAccessor.ConquerorContext is { } ctx && response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var ctxValues))
            {
                var parsedContextItems = ContextValueFormatter.Parse(ctxValues);
                ctx.AddOrReplaceItems(parsedContextItems);
            }

            if (typeof(TResponse) == typeof(UnitCommandResponse))
            {
                return (TResponse)(object)UnitCommandResponse.Instance;
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(Options.JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
            return result!;
        }

        private void SetHeaders(HttpHeaders headers)
        {
            if (Activity.Current is null && conquerorContextAccessor.ConquerorContext?.TraceId is { } traceId)
            {
                headers.Add(HttpConstants.TraceParentHeaderName, TracingHelper.CreateTraceParent(traceId: traceId));
            }

            if (conquerorContextAccessor.ConquerorContext?.HasItems ?? false)
            {
                headers.Add(HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(conquerorContextAccessor.ConquerorContext.Items));
            }

            if (commandContextAccessor.CommandContext?.CommandId is { } commandId)
            {
                headers.Add(HttpConstants.ConquerorCommandIdHeaderName, commandId);
            }

            if (Options.Headers is { } headersFromOptions)
            {
                foreach (var (headerName, headerValues) in headersFromOptions)
                {
                    headers.Add(headerName, headerValues);
                }
            }
        }
    }
}
