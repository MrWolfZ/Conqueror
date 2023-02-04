using System;
using Conqueror.CQS.Transport.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsMvcBuilderExtensions
    {
        public static IMvcBuilder AddConquerorCQSHttpControllers(this IMvcBuilder builder, Action<ConquerorCqsHttpTransportServerAspNetCoreOptions>? configureOptions = null)
        {
            _ = builder.Services.AddConquerorCQS();

            builder.Services.TryAddSingleton(new CqsHttpServerAspNetCoreRegistrationFinalizer(builder.Services));

            _ = builder.Services.PostConfigure<MvcOptions>(options => { _ = options.Filters.Add<BadContextExceptionHandlerFilter>(); });

            var options = new ConquerorCqsHttpTransportServerAspNetCoreOptions();

            configureOptions?.Invoke(options);

            _ = builder.Services.AddSingleton(options);

            return builder;
        }
    }
}
