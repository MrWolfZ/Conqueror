using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

internal sealed class ConquerorHttpServerMessagingEndpointMetadataApiDescriptionProvider(
    EndpointDataSource endpointDataSource) : IApiDescriptionProvider
{
    public int Order => 0;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        _ = endpointDataSource;
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
        foreach (var endpoint in endpointDataSource.Endpoints)
        {
            if (endpoint is RouteEndpoint routeEndpoint &&
                routeEndpoint.Metadata.GetMetadata<ConquerorHttpMessageEndpointMetadata>() is { } endpointMetadata &&
                routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>() is { } httpMethodMetadata &&
                routeEndpoint.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>() is null or { ExcludeFromDescription: false })
            {
                // REVIEW: Should we add an ApiDescription for endpoints without IHttpMethodMetadata? Swagger doesn't handle
                // a null HttpMethod even though it's nullable on ApiDescription, so we'd need to define "default" HTTP methods.
                // In practice, the Delegate will be called for any HTTP method if there is no IHttpMethodMetadata.
                foreach (var httpMethod in httpMethodMetadata.HttpMethods)
                {
                    context.Results.Add(CreateApiDescription(routeEndpoint, httpMethod, endpointMetadata));
                }
            }
        }
    }

    private ApiDescription CreateApiDescription(RouteEndpoint routeEndpoint, string httpMethod, ConquerorHttpMessageEndpointMetadata endpointMetadata)
    {
        var apiDescription = new ApiDescription
        {
            HttpMethod = httpMethod,
            GroupName = routeEndpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName,
            RelativePath = routeEndpoint.RoutePattern.RawText?.TrimStart('/'),
            ActionDescriptor = new()
            {
                DisplayName = routeEndpoint.DisplayName,
                RouteValues =
                {
                    ["controller"] = endpointMetadata.Name,
                },
            },
        };

        AddSupportedRequestFormats(apiDescription.SupportedRequestFormats, endpointMetadata);
        AddParameterDescriptions(apiDescription.ParameterDescriptions, endpointMetadata);
        AddSupportedResponseTypes(apiDescription.SupportedResponseTypes, endpointMetadata);
        AddActionDescriptorEndpointMetadata(apiDescription.ActionDescriptor, routeEndpoint.Metadata);

        return apiDescription;
    }

    private static void AddSupportedRequestFormats(
        IList<ApiRequestFormat> supportedRequestFormats,
        ConquerorHttpMessageEndpointMetadata endpointMetadata)
    {
        if (endpointMetadata.HasPayload && endpointMetadata.HttpMethod != HttpMethods.Get)
        {
            supportedRequestFormats.Add(new()
            {
                MediaType = endpointMetadata.MessageContentType,
            });
        }
    }

    private static void AddParameterDescriptions(
        IList<ApiParameterDescription> parameterDescriptions,
        ConquerorHttpMessageEndpointMetadata endpointMetadata)
    {
        if (!endpointMetadata.HasPayload)
        {
            return;
        }

        foreach (var (name, type) in endpointMetadata.QueryParams)
        {
            parameterDescriptions.Add(new()
            {
                Name = name,
                ModelMetadata = new EndpointModelMetadata(ModelMetadataIdentity.ForType(type)),
                Source = BindingSource.Query,
                Type = type,
                IsRequired = true,
            });
        }

        if (endpointMetadata.HttpMethod == ConquerorTransportHttpConstants.MethodGet)
        {
            return;
        }

        parameterDescriptions.Add(new()
        {
            Name = endpointMetadata.MessageType.Name,
            ModelMetadata = new EndpointModelMetadata(ModelMetadataIdentity.ForType(endpointMetadata.MessageType)),
            Source = BindingSource.Body,
            Type = endpointMetadata.MessageType,
            IsRequired = true,
        });
    }

    private static void AddSupportedResponseTypes(
        IList<ApiResponseType> supportedResponseTypes,
        ConquerorHttpMessageEndpointMetadata endpointMetadata)
    {
        // TODO: add proper error metadata
        // var errorMetadata = endpointMetadata.GetMetadata<ProducesErrorResponseTypeAttribute>();
        // var defaultErrorType = errorMetadata?.Type ?? typeof(void);

        if (endpointMetadata.ResponseType == typeof(UnitMessageResponse))
        {
            supportedResponseTypes.Add(new()
            {
                StatusCode = endpointMetadata.SuccessStatusCode,
                Type = typeof(void),
                ApiResponseFormats = [],
                IsDefaultResponse = true,
            });

            return;
        }

        supportedResponseTypes.Add(new()
        {
            StatusCode = endpointMetadata.SuccessStatusCode,
            Type = endpointMetadata.ResponseType,
            ApiResponseFormats =
            [
                new()
                {
                    MediaType = endpointMetadata.ResponseContentType,
                },
            ],
            IsDefaultResponse = true,
        });
    }

    private static void AddActionDescriptorEndpointMetadata(
        ActionDescriptor actionDescriptor,
        EndpointMetadataCollection endpointMetadata)
    {
        if (endpointMetadata.Count > 0)
        {
            // ActionDescriptor.EndpointMetadata is an empty array by
            // default so need to add the metadata into a new list.
            actionDescriptor.EndpointMetadata = new List<object>(endpointMetadata);
        }
    }

    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "abstract properties are not used")]
    private sealed class EndpointModelMetadata(ModelMetadataIdentity identity) : ModelMetadata(identity)
    {
        public override IReadOnlyDictionary<object, object> AdditionalValues { get; } = ImmutableDictionary<object, object>.Empty;
        public override ModelPropertyCollection Properties { get; } = new([]);
        public override string? BinderModelName { get; }
        public override Type? BinderType { get; }
        public override BindingSource? BindingSource { get; }
        public override bool ConvertEmptyStringToNull { get; }
        public override string? DataTypeName { get; }
        public override string? Description { get; }
        public override string? DisplayFormatString { get; }
        public override string? DisplayName { get; }
        public override string? EditFormatString { get; }
        public override ModelMetadata? ElementMetadata { get; }
        public override IEnumerable<KeyValuePair<EnumGroupAndName, string>>? EnumGroupedDisplayNamesAndValues { get; }
        public override IReadOnlyDictionary<string, string>? EnumNamesAndValues { get; }
        public override bool HasNonDefaultEditFormat { get; }
        public override bool HtmlEncode { get; }
        public override bool HideSurroundingHtml { get; }
        public override bool IsBindingAllowed { get; } = true;
        public override bool IsBindingRequired { get; }
        public override bool IsEnum { get; }
        public override bool IsFlagsEnum { get; }
        public override bool IsReadOnly { get; }
        public override bool IsRequired { get; }
        public override ModelBindingMessageProvider ModelBindingMessageProvider { get; } = new DefaultModelBindingMessageProvider();
        public override int Order { get; }
        public override string? Placeholder { get; }
        public override string? NullDisplayText { get; }
        public override IPropertyFilterProvider? PropertyFilterProvider { get; }
        public override bool ShowForDisplay { get; }
        public override bool ShowForEdit { get; }
        public override string? SimpleDisplayProperty { get; }
        public override string? TemplateHint { get; }
        public override bool ValidateChildren { get; }
        public override IReadOnlyList<object> ValidatorMetadata { get; } = Array.Empty<object>();
        public override Func<object, object?>? PropertyGetter { get; }
        public override Action<object, object?>? PropertySetter { get; }
    }
}

internal sealed record ConquerorHttpMessageEndpointMetadata
{
    public required string Name { get; init; }
    public required string HttpMethod { get; init; }
    public required Type MessageType { get; init; }

    public required bool HasPayload { get; init; }

    public required IReadOnlyCollection<(string Name, Type Type)> QueryParams { get; init; }

    public required Type ResponseType { get; init; }

    public required int SuccessStatusCode { get; init; }

    public required string MessageContentType { get; init; }

    public required string ResponseContentType { get; init; }
}
