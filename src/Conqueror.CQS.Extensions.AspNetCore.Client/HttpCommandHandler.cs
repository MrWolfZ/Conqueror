using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common;

// it makes sense for these classes to be in the same file
#pragma warning disable SA1402

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class HttpCommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : class
    {
        private readonly ConquerorClientContext conquerorClientContext;
        private readonly IConquerorContextAccessor? conquerorContextAccessor;
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions? serializerOptions;

        public HttpCommandHandler(ResolvedHttpClientOptions options, ConquerorClientContext conquerorClientContext, IConquerorContextAccessor? conquerorContextAccessor)
        {
            this.conquerorClientContext = conquerorClientContext;
            this.conquerorContextAccessor = conquerorContextAccessor;
            httpClient = options.HttpClient;
            serializerOptions = options.JsonSerializerOptions;
        }

        public async Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            using var content = JsonContent.Create(command, null, serializerOptions);

            var contextItems = new Dictionary<string, string>();

            if (conquerorClientContext.HasItems)
            {
                contextItems.AddOrReplaceRange(conquerorClientContext.GetItems());
            }

            contextItems.AddOrReplaceRange(conquerorContextAccessor?.ConquerorContext?.Items ?? Enumerable.Empty<KeyValuePair<string, string>>());

            if (contextItems.Count > 0)
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

            if (response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values))
            {
                var parsedContextItems = ContextValueFormatter.Parse(values);

                if (conquerorClientContext.IsActive)
                {
                    conquerorClientContext.AddOrReplaceItems(parsedContextItems);
                }

                if (conquerorContextAccessor?.ConquerorContext is { } ctx)
                {
                    ctx.AddOrReplaceItems(parsedContextItems);
                }
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(serializerOptions, cancellationToken);
            return result!;
        }
    }

    internal sealed class HttpCommandHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : class
    {
        private readonly ConquerorClientContext conquerorClientContext;
        private readonly IConquerorContextAccessor? conquerorContextAccessor;
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions? serializerOptions;

        public HttpCommandHandler(ResolvedHttpClientOptions options, ConquerorClientContext conquerorClientContext, IConquerorContextAccessor? conquerorContextAccessor)
        {
            this.conquerorClientContext = conquerorClientContext;
            this.conquerorContextAccessor = conquerorContextAccessor;
            httpClient = options.HttpClient;
            serializerOptions = options.JsonSerializerOptions;
        }

        public async Task ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            using var content = JsonContent.Create(command, null, serializerOptions);

            var contextItems = new Dictionary<string, string>();

            if (conquerorClientContext.HasItems)
            {
                contextItems.AddOrReplaceRange(conquerorClientContext.GetItems());
            }

            contextItems.AddOrReplaceRange(conquerorContextAccessor?.ConquerorContext?.Items ?? Enumerable.Empty<KeyValuePair<string, string>>());

            if (contextItems.Count > 0)
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

            if (response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values))
            {
                var parsedContextItems = ContextValueFormatter.Parse(values);

                if (conquerorClientContext.IsActive)
                {
                    conquerorClientContext.AddOrReplaceItems(parsedContextItems);
                }

                if (conquerorContextAccessor?.ConquerorContext is { } ctx)
                {
                    ctx.AddOrReplaceItems(parsedContextItems);
                }
            }
        }
    }
}
