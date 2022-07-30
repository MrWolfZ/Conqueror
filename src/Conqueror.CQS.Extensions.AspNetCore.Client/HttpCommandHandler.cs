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
        private readonly CommandClientContext commandClientContext;
        private readonly ICommandContextAccessor? commandContextAccessor;
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions? serializerOptions;

        public HttpCommandHandler(ResolvedHttpClientOptions options, CommandClientContext commandClientContext, ICommandContextAccessor? commandContextAccessor)
        {
            this.commandClientContext = commandClientContext;
            this.commandContextAccessor = commandContextAccessor;
            httpClient = options.HttpClient;
            serializerOptions = options.JsonSerializerOptions;
        }

        public async Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            _ = commandContextAccessor;

            using var content = JsonContent.Create(command, null, serializerOptions);

            var contextItems = new Dictionary<string, string>();

            if (commandClientContext.HasItems)
            {
                contextItems.SetRange(commandClientContext.Items);
            }

            contextItems.SetRange(commandContextAccessor?.CommandContext?.TransferrableItems ?? Enumerable.Empty<KeyValuePair<string, string>>());

            if (contextItems.Count > 0)
            {
                content.Headers.Add(HttpConstants.CommandContextHeaderName, ContextValueFormatter.Format(contextItems));
            }

            var regex = new Regex("Command$");
            var routeCommandName = regex.Replace(typeof(TCommand).Name, string.Empty);
            var response = await httpClient.PostAsync(new Uri($"/api/commands/{routeCommandName}", UriKind.Relative), content, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                var responseContent = await response.BufferAndReadContent();
                throw new HttpCommandFailedException($"command of type {typeof(TCommand).Name} failed: {responseContent}", response);
            }

            if (response.Headers.TryGetValues(HttpConstants.CommandContextHeaderName, out var values))
            {
                var parsedContextItems = ContextValueFormatter.Parse(values);

                if (commandClientContext.CaptureIsEnabled)
                {
                    commandClientContext.AddResponseItems(parsedContextItems);
                }

                if (commandContextAccessor?.CommandContext is { } ctx)
                {
                    ctx.AddTransferrableItems(parsedContextItems);
                }
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(serializerOptions, cancellationToken);
            return result!;
        }
    }

    internal sealed class HttpCommandHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : class
    {
        private readonly CommandClientContext commandClientContext;
        private readonly ICommandContextAccessor? commandContextAccessor;
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions? serializerOptions;

        public HttpCommandHandler(ResolvedHttpClientOptions options, CommandClientContext commandClientContext, ICommandContextAccessor? commandContextAccessor)
        {
            this.commandClientContext = commandClientContext;
            this.commandContextAccessor = commandContextAccessor;
            httpClient = options.HttpClient;
            serializerOptions = options.JsonSerializerOptions;
        }

        public async Task ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            using var content = JsonContent.Create(command, null, serializerOptions);

            var contextItems = new Dictionary<string, string>();

            if (commandClientContext.HasItems)
            {
                contextItems.SetRange(commandClientContext.Items);
            }

            contextItems.SetRange(commandContextAccessor?.CommandContext?.TransferrableItems ?? Enumerable.Empty<KeyValuePair<string, string>>());

            if (contextItems.Count > 0)
            {
                content.Headers.Add(HttpConstants.CommandContextHeaderName, ContextValueFormatter.Format(contextItems));
            }

            var regex = new Regex("Command$");
            var routeCommandName = regex.Replace(typeof(TCommand).Name, string.Empty);
            var response = await httpClient.PostAsync(new Uri($"/api/commands/{routeCommandName}", UriKind.Relative), content, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                var responseContent = await response.BufferAndReadContent();
                throw new HttpCommandFailedException($"command of type {typeof(TCommand).Name} failed: {responseContent}", response);
            }

            if (response.Headers.TryGetValues(HttpConstants.CommandContextHeaderName, out var values))
            {
                var parsedContextItems = ContextValueFormatter.Parse(values);

                if (commandClientContext.CaptureIsEnabled)
                {
                    commandClientContext.AddResponseItems(parsedContextItems);
                }

                if (commandContextAccessor?.CommandContext is { } ctx)
                {
                    ctx.AddTransferrableItems(parsedContextItems);
                }
            }
        }
    }
}
