using System;
using System.Diagnostics;
using System.Net;
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
        private static readonly DefaultHttpCommandPathConvention DefaultCommandPathConvention = new();
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

            if (Activity.Current is null && conquerorContextAccessor.ConquerorContext?.TraceId is { } traceId)
            {
                content.Headers.Add(HttpConstants.ConquerorTraceIdHeaderName, traceId);
            }

            if (conquerorContextAccessor.ConquerorContext?.HasItems ?? false)
            {
                content.Headers.Add(HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(conquerorContextAccessor.ConquerorContext.Items));
            }

            if (commandContextAccessor.CommandContext?.CommandId is { } commandId)
            {
                content.Headers.Add(HttpConstants.ConquerorCommandIdHeaderName, commandId);
            }

            var path = Options.CommandPathConvention?.GetCommandPath(typeof(TCommand), attribute) ?? DefaultCommandPathConvention.GetCommandPath(typeof(TCommand), attribute);

            var response = await Options.HttpClient.PostAsync(new Uri(path, UriKind.Relative), content, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                var responseContent = await response.BufferAndReadContent();
                throw new HttpCommandFailedException($"command of type {typeof(TCommand).Name} failed: {responseContent}", response);
            }

            if (conquerorContextAccessor.ConquerorContext is { } ctx && response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values))
            {
                var parsedContextItems = ContextValueFormatter.Parse(values);
                ctx.AddOrReplaceItems(parsedContextItems);
            }

            if (typeof(TResponse) == typeof(UnitCommandResponse))
            {
                return (TResponse)(object)UnitCommandResponse.Instance;
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(Options.JsonSerializerOptions, cancellationToken);
            return result!;
        }
    }
}
