#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpServerMessagingEndpointRouteBuilderExtensions
{
    private static readonly EndpointTypeInjectable EndpointConfigurationInjectable = new();

    public static IEndpointRouteBuilder MapMessageEndpoints(this IEndpointRouteBuilder builder)
    {
        var messageTransportRegistry = builder.ServiceProvider.GetRequiredService<IMessageHandlerRegistry>();
        foreach (var invoker in messageTransportRegistry.GetReceiverHandlerInvokers<IHttpMessageHandlerTypesInjector>())
        {
            _ = invoker.TypesInjector.Inject(EndpointConfigurationInjectable, new(builder, invoker));
        }

        return builder;
    }

    public static IEndpointConventionBuilder? MapMessageEndpoint<TMessage, TResponse, TIHandler>(
        this IEndpointRouteBuilder builder
    )
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler> =>
        builder.MapMessageEndpoint(new MessageTypes<TMessage, TResponse, TIHandler>());

    public static IEndpointConventionBuilder? MapMessageEndpoint<TMessage, TResponse, TIHandler>(
        this IEndpointRouteBuilder builder,
        MessageTypes<TMessage, TResponse, TIHandler> _
    )
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        var handlerRegistry = builder.ServiceProvider.GetRequiredService<IMessageHandlerRegistry>();
        var invoker = handlerRegistry.GetReceiverHandlerInvoker<
            TMessage,
            TResponse,
            IHttpMessageHandlerTypesInjector
        >();

        return invoker is null
            ? throw new InvalidOperationException(
                $"no handler is registered for HTTP message type '{typeof(TMessage)}'"
            )
            : invoker.TypesInjector.Inject(EndpointConfigurationInjectable, new(builder, invoker));
    }

    private readonly record struct EndpointTypeInjectableArg(
        IEndpointRouteBuilder Builder,
        IMessageReceiverHandlerInvoker<IHttpMessageHandlerTypesInjector> Invoker
    );

    private sealed class EndpointTypeInjectable
        : IHttpMessageTypesInjectable<EndpointTypeInjectableArg, IEndpointConventionBuilder?>
    {
        IEndpointConventionBuilder? IHttpMessageTypesInjectable<
            EndpointTypeInjectableArg,
            IEndpointConventionBuilder?
        >.WithInjectedTypes<TMessage, TResponse, TIHandler>(EndpointTypeInjectableArg arg)
        {
            var receiver = new HttpMessageReceiver<TMessage, TResponse>(
                arg.Invoker.HandlerType,
                arg.Builder.ServiceProvider
            );

            try
            {
                arg.Invoker.TypesInjector.ConfigureHttpReceiver(receiver);
            }
            catch (Exception ex)
            {
                throw new MessageReceiverExecutionFailedException(
                    $"failed to configure HTTP endpoint for message type '{typeof(TMessage)}' and handler type '{arg.Invoker.HandlerType}'",
                    ex
                )
                {
                    HandlerType = receiver.HandlerType,
                    MessageTransportType = new MessageTransportType(TransportName, MessageTransportRole.Receiver),
                };
            }

            if (!receiver.IsEnabled)
            {
                return null;
            }

            var duplicates = arg
                .Builder.DataSources.SelectMany(ds => ds.Endpoints)
                .SelectMany(e => e.Metadata)
                .OfType<ConquerorHttpMessageEndpointMetadata>()
                .Where(m =>
                    string.Equals(m.FullPath, TMessage.FullPath, StringComparison.Ordinal)
                    && string.Equals(m.HttpMethod, TMessage.HttpMethod, StringComparison.Ordinal)
                )
                .ToList();

            if (duplicates.Count > 0)
            {
                var duplicateMessageTypes = duplicates.Select(d => d.MessageType).Concat([typeof(TMessage)]);
                var msg =
                    $"path: {TMessage.FullPath}{Environment.NewLine}messageTypes:{Environment.NewLine}{string.Join(Environment.NewLine, duplicateMessageTypes)}";

                throw new InvalidOperationException(
                    $"found multiple Conqueror message types with identical path!{Environment.NewLine}{msg}"
                );
            }

            return ConfigureRoute<TMessage, TResponse>(
                arg.Builder.MapMethods(
                    TMessage.FullPath,
                    [TMessage.HttpMethod],
                    ctx => Handle<TMessage, TResponse, TIHandler>(ctx, arg.Invoker)
                ),
                TMessage.EmptyInstance is null,
                receiver.IsOmittedFromApiDescription
            );
        }

        private async Task Handle<TMessage, TResponse, TIHandler>(
            HttpContext context,
            IMessageReceiverHandlerInvoker invoker
        )
            where TMessage : class, IHttpMessage<TMessage, TResponse>
            where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        {
            var message = TMessage.EmptyInstance;

            var mediaType = GetMediaTypeFromContentType(context.Request.ContentType);

            var expectedContentType = message is null ? TMessage.HttpMessageSerializer.ContentType : "";
            if (!string.Equals(mediaType?.MediaType ?? "", expectedContentType, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;

                if (context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() ?? false)
                {
                    await context
                        .Response.WriteAsync(
                            $"invalid request; expected content type '{expectedContentType}' but got '{mediaType?.MediaType}'",
                            CancellationToken.None
                        )
                        .ConfigureAwait(false);
                }

                return;
            }

            // handle messages without payload
            if (message is not null)
            {
                await Handle<TMessage, TResponse, TIHandler>(message, context, invoker).ConfigureAwait(false);

                return;
            }

            var encoding = string.IsNullOrWhiteSpace(mediaType?.CharSet)
                ? null
                : Encoding.GetEncoding(mediaType.CharSet);

            var query = context.Request.Query.Select(p => new KeyValuePair<string, IReadOnlyList<string?>>(
                p.Key,
                p.Value
            ));

            message = await TMessage
                .HttpMessageSerializer.DeserializeMessage(
                    context.RequestServices,
                    context.Request.Body,
                    encoding,
                    context.Request.Path,
                    query,
                    context.RequestAborted
                )
                .ConfigureAwait(false);

            await Handle<TMessage, TResponse, TIHandler>(message, context, invoker).ConfigureAwait(false);
        }

        private async Task Handle<TMessage, TResponse, TIHandler>(
            TMessage? message,
            HttpContext httpContext,
            IMessageReceiverHandlerInvoker invoker
        )
            where TMessage : class, IHttpMessage<TMessage, TResponse>
            where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        {
            if (message is null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                if (httpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() ?? false)
                {
                    await httpContext
                        .Response.WriteAsync("invalid request; empty message", CancellationToken.None)
                        .ConfigureAwait(false);
                }

                return;
            }

            using var conquerorContext = httpContext
                .RequestServices.GetRequiredService<IConquerorContextAccessor>()
                .CloneOrCreate();

            try
            {
                conquerorContext.DecodeContextData(ReadContextDataFromRequest(httpContext));
            }
            catch (FormattedConquerorContextDataInvalidException ex)
            {
                throw new MessageFailedDueToInvalidFormattedConquerorContextDataException(
                    $"badly formatted context data while processing HTTP {nameof(message)} of type '{typeof(TMessage)}'",
                    ex
                )
                {
                    MessagePayload = message,
                    TransportType = new MessageTransportType(TransportName, MessageTransportRole.Receiver),
                };
            }

            if (GetTraceId(httpContext) is { } traceId)
            {
                conquerorContext.TraceId = traceId;
            }

            using var principal = conquerorContext.SetCurrentPrincipalInternal(httpContext.User);

            var response = await invoker
                .Invoke<TMessage, TResponse>(
                    message,
                    httpContext.RequestServices,
                    TransportName,
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);

            httpContext.Response.StatusCode = TMessage.SuccessStatusCode;

            if (conquerorContext.EncodeUpstreamContextData() is { } data)
            {
                httpContext.Response.Headers[HeaderNames.ConquerorContext] = data;
            }

            if (typeof(TResponse) == typeof(UnitMessageResponse))
            {
                return;
            }

            httpContext.Response.Headers.ContentType = TMessage.HttpMessageResponseSerializer.ContentType;

            await TMessage
                .HttpMessageResponseSerializer.SerializeResponse(
                    httpContext.RequestServices,
                    httpContext.Response.Body,
                    response,
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);

            static IEnumerable<string> ReadContextDataFromRequest(HttpContext httpContext)
            {
                return httpContext.Request.Headers.TryGetValue(HeaderNames.ConquerorContext, out var values)
                    ? values
                    : [];
            }

            static string? GetTraceId(HttpContext httpContext)
            {
                var activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity ?? Activity.Current;
                if (activity?.TraceId.ToString() is { } s)
                {
                    return s;
                }

                if (
                    httpContext.Request.Headers.TryGetValue(HeaderNames.TraceParent, out var traceParentValues)
                    && traceParentValues is [{ } traceParent]
                )
                {
                    return ActivityTraceId.CreateFromString(traceParent.AsSpan(start: 3, length: 32)).ToHexString();
                }

                return null;
            }
        }

        private static IEndpointConventionBuilder ConfigureRoute<TMessage, TResponse>(
            IEndpointConventionBuilder builder,
            bool hasPayload,
            bool isOmittedFromApiDescription
        )
            where TMessage : class, IHttpMessage<TMessage, TResponse>
        {
            builder = builder
                .WithMetadata(
                    typeof(TResponse) == typeof(UnitMessageResponse)
                        ? new ProducesResponseTypeMetadata(TMessage.SuccessStatusCode)
                        : new(
                            TMessage.SuccessStatusCode,
                            typeof(TResponse),
                            [TMessage.HttpMessageResponseSerializer.ContentType]
                        )
                )
                .WithMetadata(
                    new ConquerorHttpMessageEndpointMetadata
                    {
                        Name = TMessage.Name,
                        FullPath = TMessage.FullPath,
                        ApiGroupName = TMessage.ApiGroupName,
                        HttpMethod = TMessage.HttpMethod,
                        MessageContentType = TMessage.HttpMessageSerializer.ContentType,
                        ResponseContentType = TMessage.HttpMessageResponseSerializer.ContentType,
                        MessageType = typeof(TMessage),
                        HasPayload = hasPayload,
                        QueryParams = string.Equals(TMessage.HttpMethod, MethodNames.Get, StringComparison.Ordinal)
                            ? GetQueryParams()
                            : [],
                        ResponseType = typeof(TResponse),
                        SuccessStatusCode = TMessage.SuccessStatusCode,
                    }
                )
                .WithName(TMessage.Name);

            if (isOmittedFromApiDescription)
            {
                builder = builder.WithMetadata(new ExcludeFromDescriptionAttribute());
            }

            builder.Finally(b =>
            {
                var defaultResponseTypeMetadata = b
                    .Metadata.OfType<ProducesResponseTypeMetadata>()
                    .FirstOrDefault(m => m.Type == typeof(object));

                if (defaultResponseTypeMetadata is null)
                {
                    return;
                }

                var index = b.Metadata.IndexOf(defaultResponseTypeMetadata);
                b.Metadata.RemoveAt(index);
            });

            return TMessage.ApiGroupName is null ? builder : builder.WithGroupName(TMessage.ApiGroupName);

            static IReadOnlyCollection<HttpMessageEndpointQueryParameterMetadata> GetQueryParams()
            {
                if (TMessage.PublicConstructors.Any(c => c.GetParameters().Length is 0))
                {
                    return TMessage
                        .PublicProperties.Select(p => new HttpMessageEndpointQueryParameterMetadata
                        {
                            Name = Uncapitalize(p.Name),
                            PropertyType = p.PropertyType,
                            IsRequired = p.PropertyType.GetCustomAttribute<RequiredMemberAttribute>() is not null,
                        })
                        .ToArray();
                }

                var constructor =
                    TMessage.PublicConstructors.FirstOrDefault()
                    ?? throw new InvalidOperationException(
                        $"no public constructor found for message type '{typeof(TMessage)}'"
                    );

                return constructor
                    .GetParameters()
                    .Where(p => p.Name is not null)
                    .Select(p => new HttpMessageEndpointQueryParameterMetadata
                    {
                        Name = Uncapitalize(p.Name!),
                        PropertyType = p.ParameterType,
                        IsRequired = !p.HasDefaultValue,
                    })
                    .ToArray();
            }

            static string Uncapitalize(string str)
            {
                return char.ToLower(str[0], CultureInfo.InvariantCulture) + str[1..];
            }
        }

        private static MediaTypeHeaderValue? GetMediaTypeFromContentType(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return null;
            }

            try
            {
                return MediaTypeHeaderValue.Parse(contentType);
            }
            catch
            {
                // Ignore and fall back to default
                return null;
            }
        }
    }
}
